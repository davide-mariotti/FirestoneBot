using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using MelonLoader;
using PathOfGlory = Firebot.GameModel.Features.BattlePass.BattlePass;

namespace Firebot.Tasks.BattlePass;

/// <summary>
///     Claims every Path of Glory (battle pass) reward earned so far: the free track, and the Golden
///     track when the pass is owned. Never buys the pass or skips ahead.
/// </summary>
public class PathOfGloryTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    private MelonPreferences_Entry<int> _recheckIntervalMinutes;

    // The badge sits on the HUD button itself, which moves between HUD variants.
    protected override string[] NotificationPaths => new[]
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
