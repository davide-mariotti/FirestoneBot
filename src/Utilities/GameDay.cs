using System;

namespace Firebot.Utilities;

/// <summary>
///     The game's daily reset (quests and every "already done today" tracker tied to them) happens at
///     10:00 local time, not midnight - so between midnight and 10:00 it's still the previous game-day.
/// </summary>
public static class GameDay
{
    private const int ResetHour = 10;

    /// <summary>The current game-day as "yyyy-MM-dd".</summary>
    public static string Today()
    {
        var now = DateTime.Now;
        var gameDay = now.Hour < ResetHour ? now.Date.AddDays(-1) : now.Date;
        return gameDay.ToString("yyyy-MM-dd");
    }

    /// <summary>The next 10:00, when a new game-day starts.</summary>
    public static DateTime NextReset()
    {
        var reset = DateTime.Today.AddHours(ResetHour);
        return DateTime.Now < reset ? reset : reset.AddDays(1);
    }
}
