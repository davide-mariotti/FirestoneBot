using System;

namespace Firebot.Utilities;

/// <summary>
///     The game's daily reset (quests and every "already done today" tracker tied to them) happens at
///     10:00 local time, not midnight - so between midnight and 10:00 it's still the previous game-day.
///     The bot starts its day 2 minutes later: right at 10:00 the game still shows yesterday's quests
///     for up to a minute (01/10) and yesterday's liberation missions for ~20 s (03/10).
/// </summary>
public static class GameDay
{
    private static readonly TimeSpan GameReset = TimeSpan.FromHours(10);
    private static readonly TimeSpan Reset = GameReset + TimeSpan.FromMinutes(2);

    // A countdown read to the second can land a few seconds either side of 10:00.
    private static readonly TimeSpan CountdownSlack = TimeSpan.FromSeconds(30);

    /// <summary>The current game-day as "yyyy-MM-dd".</summary>
    public static string Today()
    {
        var now = DateTime.Now;
        var gameDay = now.TimeOfDay < Reset ? now.Date.AddDays(-1) : now.Date;
        return gameDay.ToString("yyyy-MM-dd");
    }

    /// <summary>The next 10:02, when the bot's game-day starts.</summary>
    public static DateTime NextReset()
    {
        var reset = DateTime.Today + Reset;
        return DateTime.Now < reset ? reset : reset.AddDays(1);
    }

    /// <summary>A run time that falls on the reset - a game countdown to 10:00 - moves to 10:02.</summary>
    public static DateTime PastReset(DateTime time)
    {
        if (time.Date == DateTime.MaxValue.Date) return time;
        var reset = time.Date + Reset;
        return time >= time.Date + GameReset - CountdownSlack && time < reset ? reset : time;
    }
}
