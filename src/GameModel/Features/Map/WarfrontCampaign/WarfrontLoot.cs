using System;
using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Map.WarfrontCampaign;

public static class WarfrontLoot
{
    public static IEnumerator Claim => new GameButton(Paths.WorldMapLoc.WarfrontLoc.ClaimBtn).Click();

    public static DateTime NextRunTime => new GameText(Paths.WorldMapLoc.WarfrontLoc.NextRunTimeTxt).Time;
}
