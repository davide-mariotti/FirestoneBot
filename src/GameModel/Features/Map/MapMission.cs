using System;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Map;

public static class MapMission
{
    public static DateTime NextRunTime => new GameText(Paths.WorldMapLoc.MapMissionsLoc.NextRunTimeTxt).Time;
}
