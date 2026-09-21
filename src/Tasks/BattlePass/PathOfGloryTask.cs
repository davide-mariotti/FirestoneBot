using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using MelonLoader;
using PathOfGlory = Firebot.GameModel.Features.BattlePass.BattlePass;

namespace Firebot.Tasks.BattlePass;

/// <summary>
///     Claims Path of Glory (Battle Pass) rewards as they unlock - both the free track (always) and
///     the Golden/premium track (only actually claims something if the player owns it; the button
///     is a safe no-op otherwise, same convention as every other claim button in this codebase).
///     Never touches getGoldenPassButton or instantCompleteNextMilestoneButton - those spend real
///     currency to buy/skip ahead, not claim what's already earned.
///     This feature was never automated before.
/// </summary>
public class PathOfGloryTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    private MelonPreferences_Entry<int> _recheckIntervalMinutes;

    // The badge lives directly on the battle-screen button itself (see Battle.cs), not on the
    // leftSideUINew rail - reliably means "something claimable" (not threshold-gated the way Free
    // Pickaxes/Empower/Meteorite Research are), so it's safe to use for priority scheduling. Three
    // candidates, not one: this button is itself split across HUD variants that switch dynamically
    // within a session (see RightSideUILoc/BottomSideUIMobileLoc/BottomSideUIDesktopLoc) -
    // confirmed live, 2026-09-17.
    protected override string[] NotificationPathCandidates => new[]
    {
        Paths.BattleLoc.RightSideUILoc.PathOfGloryNotification,
        Paths.BattleLoc.BottomSideUIMobileLoc.PathOfGloryNotification,
        Paths.BattleLoc.BottomSideUIDesktopLoc.PathOfGloryNotification
    };

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_recheckIntervalMinutes != null) return;

        _recheckIntervalMinutes = category.CreateEntry(
            "recheck_interval_minutes",
            60,
            "Recheck Interval (minutes)",
            "How often to check for newly-unlocked Battle Pass rewards when the notification badge " +
            "isn't showing (Gloria is earned through normal play, not on a fixed timer, so there's no " +
            "exact next-unlock time to schedule against). Default: 60."
        );
    }

    public override IEnumerator Execute()
    {
        // Guaranteed path - the HUD button is both the entry point and its own notification badge,
        // so unlike other tasks there's no separate opportunistic "Notifications.X" shortcut to try
        // first; this click IS the fast path.
        yield return PathOfGlory.Open;
        yield return PathOfGlory.OpenRewardsTab;

        foreach (var tier in PathOfGlory.RewardsTrack.GetChildren())
        {
            var freeClaim = new GameButton(Paths.MenusLoc.BattlePassLoc.RewardsLoc.FreeClaimBtn, tier);
            if (freeClaim.IsClickable()) yield return freeClaim.Click();

            var goldenClaim = new GameButton(Paths.MenusLoc.BattlePassLoc.RewardsLoc.GoldenClaimBtn, tier);
            if (goldenClaim.IsClickable()) yield return goldenClaim.Click();
        }

        yield return PathOfGlory.Close;

        NextRunTime = DateTime.Now + TimeSpan.FromMinutes(_recheckIntervalMinutes?.Value ?? 60);
    }
}
