using System;

namespace Firebot.Utilities;

/// <summary>
///     The game's daily reset (quests, and every per-task "already done today" tracker tied to them)
///     happens at a fixed LOCAL clock time - confirmed by the user, 2026-09-26: 10:00, not midnight.
///     Every "once per day" tracker that instead compared DateTime.Now.ToString("yyyy-MM-dd") against a
///     stored date (CollectorQuestTask/GamerQuestTask/MerchantQuestTask/MinerQuestTask) was wrong for
///     the ~10h window between midnight and the real reset: an action taken in that window got stamped
///     with the NEW calendar date, even though the game itself still counted it toward the OLD game-day
///     (which hadn't reset yet) - so once the real reset then happened, the tracker already believed
///     "today" was done/counted and skipped work the freshly-reset game-day still needed (e.g.
///     gear_chests_opened_today staying above target right after a real reset, or last_done_date already
///     matching "today" for a quest that had in fact just gone back to 0 in-game).
/// </summary>
public static class GameDay
{
    // Live-confirmed by the user, 2026-09-26: quests renew at 10:00 local time. Adjust here if this
    // ever turns out to shift (e.g. a server-side UTC reset crossing a DST boundary).
    private const int ResetHour = 10;

    /// <summary>
    ///     The current game-day as "yyyy-MM-dd" - the calendar date shifted back one day during the
    ///     hours before today's reset has actually happened.
    /// </summary>
    public static string Today()
    {
        var now = DateTime.Now;
        var gameDay = now.Hour < ResetHour ? now.Date.AddDays(-1) : now.Date;
        return gameDay.ToString("yyyy-MM-dd");
    }
}
