using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Character;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;

namespace Firebot.Tasks.Character;

/// <summary>
///     Spends talent points (Character screen, Talents tab) following the priority order in
///     GameModel/Features/Character/Talents.cs, transcribed from the user's docs/talents-guide.html.
///     This whole feature is new.
///     Fully stateless PER RUN by design: each run re-opens the guide's nodes in order (starting from
///     guide_start_index, see below) until it finds the first one still below its planned target rank,
///     invests there, and continues until either points run out or the plan is fully satisfied - it
///     never persists "where it left off" between runs beyond that one calibrated starting index.
///     Per the user's explicit choice, once every planned entry is satisfied but more points remain
///     (the guide covers only the tree's first ~420 of 2037 total points), the task stops and leaves
///     the rest unspent rather than guessing - a wrong guess isn't free (a full tree reset costs 100
///     gems per the wiki).
///     Confirmed by the user: upgradeTalentButton only stages a point, real investment happens on
///     talentsSaveButton. Saved after every single node visited (not batched across the whole plan)
///     so a later re-visit to the same talent (the plan revisits several - see Talents.Plan) always
///     reads back a real committed rank instead of needing to know whether the preview's rank display
///     reflects an unsaved pending change.
///     Guide/real-account divergence (found live, 2026-09-18, left disabled until now - see
///     TESTING.md row 30 and git history): on an account that already invested points by hand before
///     this task ever ran, blindly walking the guide from entry 0 every run means "catching up" any
///     entry still behind target, in guide order, even ones the account deliberately skipped long ago
///     - potentially spending every available point backfilling ancient history instead of the
///     account's real current frontier. Fixed by calibrating guide_start_index ONCE (see
///     CalibrateGuideStartIndex): find the longest unbroken PREFIX of Plan (from entry 0) that the
///     account's real ranks already satisfy, and only ever operate on entries from just past that
///     point onward - never re-examined, only ever advances forward, persisted so later runs skip
///     recalibrating. Per the user (2026-09-20): kept overridable - set guide_start_index directly in
///     the cfg if the auto-calibration picks a starting point that doesn't match what was actually
///     intended by hand.
///     Works fine on an account that diverged from the guide's exact order WITHIN the calibrated
///     range too, without needing to know its history: every entry only ever compares the node's
///     CURRENT live rank against that entry's target, so a talent the account already pushed ahead of
///     plan (post-calibration) reads as "nothing to invest" and is skipped, while one that's behind
///     gets topped up toward the same target it would have reached by following the guide from empty.
///     The one case this can't route around: a guide-covered talent whose OWN direct prerequisite (a
///     specific earlier talent in its branch, not just the tree's cumulative point total - see the
///     wiki) never got a single point from the account's real history stays locked no matter how many
///     points are available - skipped for this run (see IsPreviewLocked below) rather than guessed at.
/// </summary>
public class TalentsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(2);

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.TalentAvailable;

    private MelonPreferences_Entry<int> _guideStartIndex;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_guideStartIndex != null) return;

        _guideStartIndex = category.CreateEntry(
            "guide_start_index",
            -1,
            "Guide Start Index",
            "Which entry of the talent guide (Talents.Plan, 0-based) to start investing from - " +
            "entries before this are never touched. -1 (default) means 'not calibrated yet': the " +
            "next run auto-calibrates it once, by finding the longest unbroken run of guide entries " +
            "(from the very first one) the account's real talent ranks already satisfy, then starts " +
            "just past that point. Check the log after the first run and set this manually if the " +
            "auto-calibrated value doesn't match what you actually intended by hand - it is never " +
            "recalculated automatically once set."
        );
    }

    public override IEnumerator Execute()
    {
        // Fast path - safe no-op if not up.
        yield return Notifications.TalentAvailable;

        // Guaranteed path regardless of the notification - same reasoning as every other task.
        // CharacterScreen.Open() retries internally (see its own doc comment) - if it still hasn't
        // opened after that, bail out rather than proceeding to calibrate/invest against garbage
        // reads (every path would resolve as "broken", which - before this guard existed - got
        // live-confirmed to silently miscalibrate guide_start_index to 0 instead of a real value).
        yield return CharacterScreen.Open();
        if (!CharacterScreen.IsOpen)
        {
            Debug("Character screen never opened this run - skipping, will retry next scheduled run.");
            yield break;
        }

        yield return CharacterScreen.OpenTalentsTab;

        if (_guideStartIndex.Value < 0) yield return CalibrateGuideStartIndex();

        var availablePoints = Talents.AvailablePoints;
        if (availablePoints > 0)
        {
            for (var planIndex = _guideStartIndex.Value; planIndex < Talents.Plan.Length; planIndex++)
            {
                var (catalogIndex, targetRank) = Talents.Plan[planIndex];
                yield return Talents.OpenNode(catalogIndex);

                if (Talents.IsPreviewLocked)
                {
                    // Shouldn't happen on an account that only ever invested through this same
                    // plan, in order - but a divergent account's real history might never have put
                    // a point in this branch's specific predecessor (see the class doc). Skip just
                    // this one entry rather than giving up on the whole run: a later entry may
                    // still be perfectly investable.
                    yield return Talents.ClosePreview;
                    continue;
                }

                var currentRank = Talents.PreviewCurrentRank;
                var toInvest = Math.Min(targetRank - currentRank, availablePoints);
                var invested = 0;

                for (var i = 0; i < toInvest && Talents.UpgradeButton.IsClickable(); i++)
                {
                    yield return Talents.UpgradeButton.Click();
                    availablePoints--;
                    invested++;
                }

                yield return Talents.ClosePreview;

                if (invested > 0) yield return Talents.Save;

                if (availablePoints <= 0) break;
            }
        }

        yield return CharacterScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    /// <summary>
    ///     Runs ONCE ever (see guide_start_index's -1 sentinel) - opens each guide entry from the very
    ///     start in order, stopping at the first one NOT already satisfied by the account's real
    ///     ranks (or locked, which necessarily means "never invested" - rank 0 can't satisfy any
    ///     positive target). Everything before that point is assumed deliberately skipped by hand and
    ///     is never revisited; everything from that point on is handled by Execute()'s normal
    ///     top-up-if-behind loop, unchanged.
    /// </summary>
    private IEnumerator CalibrateGuideStartIndex()
    {
        var startIndex = 0;

        for (var i = 0; i < Talents.Plan.Length; i++)
        {
            var (catalogIndex, targetRank) = Talents.Plan[i];
            yield return Talents.OpenNode(catalogIndex);

            var satisfied = !Talents.IsPreviewLocked && Talents.PreviewCurrentRank >= targetRank;
            yield return Talents.ClosePreview;

            if (!satisfied) break;
            startIndex = i + 1;
        }

        _guideStartIndex.Value = startIndex;

        var description = startIndex < Talents.Plan.Length
            ? $"'{Talents.CatalogName(Talents.Plan[startIndex].CatalogIndex)}' (target rank " +
              $"{Talents.Plan[startIndex].TargetRank})"
            : "past the end of the guide - nothing left to invest";
        Debug($"Talents: calibrated guide_start_index = {startIndex} -> {description}. " +
              "Override guide_start_index in the cfg if this doesn't match what you actually invested by hand.");
    }
}
