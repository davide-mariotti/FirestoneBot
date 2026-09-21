using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using UnityEngine;
using Logger = Firebot.Core.Logger;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Spends today's Arena of Kings battle tokens (max 5, recharge daily per the wiki) fighting the
///     weakest available opponent each time, per the user's requested strategy. No prior precedent - this
///     whole feature is new. Level-gated at 80 per the wiki's Arena of Kings infobox.
///     For each token: scans the 3 current opponents' power against mine (ArenaOfKings.MyPower reads
///     "totalPower" on the battle formation, the arena-specific figure - NOT the separate
///     "battlePower" display right next to it, which is for regular campaign battles and uses a
///     different formula per the wiki). If none is strictly weaker, rerolls (free every 5s per the
///     wiki, polled via RerollBtn's own clickable
///     state instead of a fixed timer - per the user, 2026-09-18) and checks again. The user confirmed
///     losing does NOT lower rank (only winning changes anything, by swapping ranks with the
///     opponent - the wiki is explicit about this), so there's no benefit to intentionally losing -
///     the progressive fallback below exists only so a token doesn't go completely unused/wasted, not
///     to manipulate rank.
///     Search is bounded by ATTEMPT COUNT per token, not elapsed time - per the user, 2026-09-20: the
///     original wall-clock version (up to 9 real minutes of searching per token, observed taking over
///     20 minutes total for 5 tokens in a bad-luck run) was judged too slow. Reroll is free every 5s
///     per the wiki, so 12 attempts is a firm 1-minute cap per token (5 tokens/day = ~5 minutes worst
///     case for the whole search, battles themselves aside):
///     - attempts 0-3 (the first 4 looks): only a strictly-weaker opponent is acceptable.
///     - attempts 4-6: accept up to 5% stronger than me.
///     - attempts 7-8: accept up to 10% stronger.
///     - attempts 9-10: accept up to 20% stronger.
///     - attempt 11 (the 12th and last look): fight whichever of the 3 is weakest regardless of
///       margin, guaranteed - a token is never left unused.
///     Formation is set up once manually by the user (see AOKBattlePreview) - same assumption as
///     Warfront's Liberator quest, never touches changeFormationButton.
///     Overrides MaxRuntimeSeconds to the framework's own maximum (3600s, same ceiling
///     BotSettings.MaxTaskRuntime is clamped to) instead of leaning on the global setting - the search
///     itself is now tightly bounded, but a real battle's own animation/result wait (MaxBattlePolls)
///     across up to 5 tokens can still legitimately take a while, so this task keeps the extra room
///     without loosening the timeout for every other task.
///     Watchdog's cleanup sweep (runs before/after every task, unconditionally) still closes whatever
///     this leaves open if it ever does get cut off - every popup/menu here (WFMenuSelection,
///     ArenaOfKings, AOKBattlePreview, AOKBattleResult) follows the standard bg/closeButton or
///     closeButton naming Watchdog already scans for generically, nothing feature-specific needed.
/// </summary>
public class ArenaOfKingsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 80;
    internal override float? MaxRuntimeSeconds => 3600f;

    // Live-confirmed, 2026-09-20 (BotManager's notification rail diagnostic dump): the badge really
    // does toggle correctly, resolving the old "unconfirmed badge" doubt - safe to also use for
    // scheduling now. A run can legitimately take a while (see MaxRuntimeSeconds/SearchPhases above),
    // but that's unchanged whether this task is picked via the badge or its own NextRunTime - and
    // BotManager.PickNotificationTask round-robins fairly, so a long run here just costs other
    // notification-visible tasks one extra rotation, not starvation.
    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ArenaTokens;

    private static readonly WaitForSeconds RerollPollWait = new(0.5f);
    private static readonly WaitForSeconds BattlePollWait = new(2f);
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // Safety bound only - never observed a real duration. The wiki says results are determined
    // server-side and then shown as a "recording", so this is expected to resolve quickly, but
    // errs generous rather than risk cutting a real animation short (same reasoning as Liberator).
    private const int MaxBattlePolls = 150; // ~5 minutes at 2s/poll

    // Safety bound only - the wiki says reroll is free every 5s, this just guards against that
    // cooldown never clearing for some reason instead of looping forever.
    private const int MaxRerollPolls = 40; // ~20s at 0.5s/poll

    // Per the user, 2026-09-20: reroll is free every 5s, so this is a firm 1-minute cap per token
    // (see FindTarget/ChooseOpponent) - replaces the old open-ended wall-clock search.
    private const int MaxSearchAttemptsPerToken = 12;

    // (attempt count - 0-indexed, i.e. how many looks at the opponent grid have already happened -
    // below which this phase applies; max acceptable opponent power as a multiple of mine, null
    // means "strictly weaker only"). Checked in order, first match wins. ChooseOpponent forces a
    // pick on the last attempt (MaxSearchAttemptsPerToken - 1) regardless of these, so a token is
    // never left unused - this array only controls how lenient the earlier attempts are.
    private static readonly (int AttemptThreshold, double? MaxMultiplier)[] SearchPhases =
    {
        (4, null),
        (7, 1.05),
        (9, 1.10),
        (12, 1.20)
    };

    public override IEnumerator Execute()
    {
        // Fast path - opportunistic only (see Battle.cs), safe no-op if not up.
        yield return Notifications.ArenaTokens;

        // Guaranteed path regardless of the notification - same reasoning as every other task.
        // Live-confirmed, 2026-09-18: "WFMenuSelection" (popups/WFMenuSelection/bg, cards "campaign"/
        // "arena") is real and does open from BattlesBtn - the previous "path broken" error was the
        // notification fast path above already having navigated to ArenaOfKings on its own, leaving
        // no WFMenuSelection popup for this guaranteed path to click into on that run.
        yield return TownScreen.Open;
        yield return TownScreen.OpenBattles;
        yield return WFMenuSelection.OpenArena;

        while (ArenaOfKings.TokensAvailable > 0)
        {
            var slotIndex = -1;
            yield return FindTarget(index => slotIndex = index);
            if (slotIndex < 0) break; // shouldn't happen (FindTarget always eventually force-picks)

            yield return ArenaOfKings.Fight(slotIndex);

            // Live-confirmed, 2026-09-18: AOKBattlePreviewLoc.FightBtn ("bg/mask/fightButton") and
            // AOKBattleResultLoc.CloseBtn ("bg/closeButton") both resolved and clicked with no
            // errors, despite looking on-screen like a plain center icon and an "OK" button
            // respectively - internal names don't always match the displayed label.
            yield return AOKBattlePreview.Fight;

            var pollsLeft = MaxBattlePolls;
            while (!AOKBattleResult.IsVisible && pollsLeft > 0)
            {
                yield return BattlePollWait;
                pollsLeft--;
            }

            if (pollsLeft == 0)
                Logger.Warning("[ArenaOfKingsTask] Battle didn't resolve within the wait bound.");

            yield return AOKBattleResult.Close;

            // Live-confirmed, 2026-09-18: right after closing the result, the arena hub screen takes
            // a moment to redraw (its own power/opponent texts and the reroll button briefly read as
            // "hidden or inactive") - reading TokensAvailable or the opponent grid immediately caused
            // the next loop iteration to see stale/blank data. Poll for the hub to be genuinely back
            // (RerollBtn clickable) instead of a fixed timer, same reasoning as the reroll wait below.
            var stabilizePolls = MaxRerollPolls;
            while (!ArenaOfKings.RerollBtn.IsClickable() && stabilizePolls > 0)
            {
                yield return RerollPollWait;
                stabilizePolls--;
            }
        }

        yield return ArenaOfKings.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private IEnumerator FindTarget(Action<int> onFound)
    {
        var attempt = 0;

        while (true)
        {
            var myPower = ArenaOfKings.MyPower;
            var powers = ArenaOfKings.OpponentPowers();

            var chosen = ChooseOpponent(myPower, powers, attempt);
            if (chosen != null)
            {
                onFound(chosen.Value);
                yield break;
            }

            attempt++;
            yield return ArenaOfKings.Reroll;

            // Poll for the reroll cooldown to clear instead of a fixed timer - per the user,
            // 2026-09-18 (same reasoning as every other animation/cooldown wait in this codebase).
            var pollsLeft = MaxRerollPolls;
            while (!ArenaOfKings.RerollBtn.IsClickable() && pollsLeft > 0)
            {
                yield return RerollPollWait;
                pollsLeft--;
            }
        }
    }

    private static int? ChooseOpponent(double myPower, IReadOnlyList<double> powers, int attemptsSoFar)
    {
        // Last look at this token - force-pick the least-bad option so it's never left unused,
        // regardless of margin. Guarantees FindTarget's loop always terminates within
        // MaxSearchAttemptsPerToken attempts.
        if (attemptsSoFar >= MaxSearchAttemptsPerToken - 1)
            return WeakestIndex(powers);

        foreach (var (attemptThreshold, maxMultiplier) in SearchPhases)
        {
            if (attemptsSoFar >= attemptThreshold) continue; // this phase's window passed - try the next, more permissive one
            return WeakestWithin(powers, myPower, maxMultiplier);
        }

        // Unreachable given SearchPhases' last threshold == MaxSearchAttemptsPerToken and the early
        // return above already covers attempt MaxSearchAttemptsPerToken - 1 - kept as a safety net.
        return WeakestIndex(powers);
    }

    private static int? WeakestWithin(IReadOnlyList<double> powers, double myPower, double? maxMultiplier)
    {
        int? best = null;
        var bestPower = double.MaxValue;

        for (var i = 0; i < powers.Count; i++)
        {
            var acceptable = maxMultiplier == null ? powers[i] < myPower : powers[i] <= myPower * maxMultiplier.Value;
            if (!acceptable || powers[i] >= bestPower) continue;

            bestPower = powers[i];
            best = i;
        }

        return best;
    }

    private static int WeakestIndex(IReadOnlyList<double> powers)
    {
        var best = 0;
        for (var i = 1; i < powers.Count; i++)
            if (powers[i] < powers[best])
                best = i;
        return best;
    }
}
