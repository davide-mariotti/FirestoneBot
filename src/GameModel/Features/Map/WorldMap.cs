using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Map;

public static class WorldMap
{
    public static bool IsVisible => new GameElement(Paths.WorldMapLoc.CloseBtn).IsVisible();

    // The map button intermittently ignores a click on some instances, hence the retrying open.
    public static IEnumerator Open => Poll.ClickUntil(
        () => new GameButton(Paths.BattleLoc.RightSideUILoc.MapBtn), () => IsVisible, "WorldMap");

    public static IEnumerator OpenMapMissionsTab =>
        new GameButton(Paths.WorldMapLoc.MapMissionsTabBtn).Click();

    // Reaching the liberation missions depends on really landing on this tab first.
    public static IEnumerator OpenWarfrontCampaignTab => Poll.ClickUntil(
        () => new GameButton(Paths.WorldMapLoc.WarfrontCampaignTabBtn),
        () => new GameElement(Paths.WorldMapLoc.WarfrontLoc.DailyMissionsBtn).IsVisible(),
        "WorldMap Warfront tab");

    public static IEnumerator Close => new GameButton(Paths.WorldMapLoc.CloseBtn).Click();
}
