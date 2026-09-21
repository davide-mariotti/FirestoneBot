using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.BattlePass;

public static class BattlePass
{
    // Same button doubles as the battle-screen entry point and its own notification badge - there's
    // no separate leftSideUINew rail entry for this feature (confirmed against the full 48-badge
    // list), so unlike other tasks there's no separate opportunistic "Notifications.X" shortcut.
    // See UiVariantButton - this bottom-bar HUD variant switches dynamically within a session
    // (confirmed live, 2026-09-17: RightSideUILoc.PathOfGloryBtn was the one actually active,
    // alongside Store/Guild/Town/Map, in a session where neither Mobile nor Desktop below was).
    // Tries every known location instead of assuming one is "the" active one.
    public static IEnumerator Open => UiVariantButton.Click(
        new GameButton(Paths.BattleLoc.RightSideUILoc.PathOfGloryBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIMobileLoc.PathOfGloryBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIDesktopLoc.PathOfGloryBtn));

    public static IEnumerator OpenRewardsTab => new GameButton(Paths.MenusLoc.BattlePassLoc.RewardsTabBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.BattlePassLoc.CloseBtn).Click();

    public static GameElement RewardsTrack => new(Paths.MenusLoc.BattlePassLoc.RewardsLoc.TrackRoot);
}
