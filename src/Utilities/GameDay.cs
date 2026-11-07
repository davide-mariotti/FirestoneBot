using System;

namespace Firebot.Utilities;

/// <summary>
///     The game's daily reset (quests and every "already done today" tracker tied to them). Before it,
///     it's still the previous game-day. The hour is the game's to change: 08:00 UTC until the 06/11
///     update, 09:00 UTC since, which keeps it at 10:00 Italian time in winter too.
///     The bot starts its day 2 minutes later: right at the reset the game still shows yesterday's quests
///     for up to a minute (01/10) and yesterday's liberation missions for ~20 s (03/10).
/// </summary>
public static class GameDay
{
    /// <summary>BotManager keeps this on TimeLibrary.officialServerTimeResetHour; this class has no game types.</summary>
    public static int ResetHourUtc { get; set; } = 9;

    private static TimeSpan GameReset => TimeSpan.FromHours(ResetHourUtc);
    private static TimeSpan Reset => GameReset + TimeSpan.FromMinutes(2);

    // A countdown read to the second can land a few seconds either side of the reset.
    private static readonly TimeSpan CountdownSlack = TimeSpan.FromSeconds(30);

    /// <summary>The current game-day as "yyyy-MM-dd".</summary>
    public static string Today()
    {
        var now = DateTime.UtcNow;
        var gameDay = now.TimeOfDay < Reset ? now.Date.AddDays(-1) : now.Date;
        return gameDay.ToString("yyyy-MM-dd");
    }

    /// <summary>The next start of the bot's game-day, in local time.</summary>
    public static DateTime NextReset() => NextUtc(Reset);

    /// <summary>The next time of day <paramref name="utcTime" /> in UTC, in local time.</summary>
    public static DateTime NextUtc(TimeSpan utcTime)
    {
        var at = DateTime.UtcNow.Date + utcTime;
        return (DateTime.UtcNow < at ? at : at.AddDays(1)).ToLocalTime();
    }

    /// <summary>A run time that falls on the reset - a game countdown to it - moves to the bot's, 2 minutes later.</summary>
    public static DateTime PastReset(DateTime time)
    {
        if (time.Date == DateTime.MaxValue.Date || time == DateTime.MinValue) return time;
        var utc = time.ToUniversalTime();
        var reset = utc.Date + Reset;
        return utc >= utc.Date + GameReset - CountdownSlack && utc < reset ? reset.ToLocalTime() : time;
    }
}
