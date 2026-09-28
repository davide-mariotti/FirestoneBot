using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.BattlePass;

public static class BattlePass
{
    // The HUD button doubles as its own notification badge (see PathOfGloryTask).
    public static IEnumerator Open => UiVariantButton.Click(
        new GameButton(Paths.BattleLoc.RightSideUILoc.PathOfGloryBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIMobileLoc.PathOfGloryBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIDesktopLoc.PathOfGloryBtn));

    public static IEnumerator OpenRewardsTab => new GameButton(Paths.MenusLoc.BattlePassLoc.RewardsTabBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.BattlePassLoc.CloseBtn).Click();

    public static GameElement RewardsTrack => new(Paths.MenusLoc.BattlePassLoc.RewardsLoc.TrackRoot);
}
