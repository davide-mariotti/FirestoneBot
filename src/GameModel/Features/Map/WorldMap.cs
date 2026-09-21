using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Map;

public static class WorldMap
{
    public static IEnumerator Open => new GameButton(Paths.BattleLoc.RightSideUILoc.MapBtn).Click();

    public static IEnumerator OpenMapMissionsTab =>
        new GameButton(Paths.WorldMapLoc.MapMissionsTabBtn).Click();

    public static IEnumerator OpenWarfrontCampaignTab =>
        new GameButton(Paths.WorldMapLoc.WarfrontCampaignTabBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.WorldMapLoc.CloseBtn).Click();
}
