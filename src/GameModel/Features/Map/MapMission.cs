using System;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Il2Cpp;

namespace Firebot.GameModel.Features.Map;

public static class MapMission
{
    public static DateTime NextRunTime => new GameText(Paths.WorldMapLoc.MapMissionsLoc.NextRunTimeTxt).Time;

    /// <summary>Squads free right now, from the game's own data (the map's "1/5"); -1 when unreadable.</summary>
    public static int FreeSquads => GameInitialize.HandlerLoader?.MissionsHandler?.availableSquads.GetDecrypted() ?? -1;
}
