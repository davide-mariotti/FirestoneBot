using System;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core;
using Il2Cpp;

namespace Firebot.GameModel.Shared;

/// <summary>
///     The game's own state, read from its objects instead of off the screen (GAMESTATE_PLAN.md).
///     Every read is fail-soft: a game update can rename a field, and the bot's method touching it then
///     throws when it is compiled. Each read therefore runs inside Get/GetRef, returns null on any
///     failure (logged once per session as "[GameState] fallback ...") and the caller goes back to the
///     screen, whose code stays in place.
/// </summary>
public static class GameState
{
    private static readonly HashSet<string> Reported = new();

    internal static HandlerLoader H => GameInitialize.HandlerLoader;
    internal static CoreFunctionalityLoader Core => GameInitialize.CoreFunctionality;

    public static T? Get<T>(string what, Func<T> read) where T : struct
    {
        try
        {
            return read();
        }
        catch (Exception e)
        {
            Report(what, e);
            return null;
        }
    }

    public static T GetRef<T>(string what, Func<T> read) where T : class
    {
        try
        {
            return read();
        }
        catch (Exception e)
        {
            Report(what, e);
            return null;
        }
    }

    private static void Report(string what, Exception e)
    {
        if (Reported.Add(what)) Logger.Info($"[GameState] fallback {what}: {e.GetType().Name} - {e.Message}");
    }

    /// <summary>Logged only when the two differ: the free check of rule 3, where the screen is open anyway.</summary>
    public static void Compare(string what, string game, string ui)
    {
        if (game != null && ui != null && game != ui) Logger.Info($"[GameState] mismatch {what}: game={game} ui={ui}");
    }

    /// <summary>Compare for times: a mismatch only past `toleranceMinutes` (the screen shows minutes, not seconds).</summary>
    public static void CompareTimes(string what, DateTime? game, DateTime ui, double toleranceMinutes = 2)
    {
        if (game is { } g && ui != DateTime.MinValue && Math.Abs((g - ui).TotalMinutes) > toleranceMinutes)
            Logger.Info($"[GameState] mismatch {what}: game={g:dd/MM HH:mm:ss} ui={ui:dd/MM HH:mm:ss}");
    }

    // ---- Clock. Server timestamps are Unix seconds (probe, 26/11: serverTimeNow 7 s off the PC's UTC).

    public static double? ServerNow => Get("server clock", () =>
        Core.ServerUpdate is { gotServerTime: true } su ? su.serverTimeNow : throw new InvalidOperationException("no server time yet"));

    /// <summary>The local time at which a server timestamp falls.</summary>
    public static DateTime? At(double serverSeconds) =>
        ServerNow is { } now ? DateTime.Now.AddSeconds(serverSeconds - now) : null;

    /// <summary>The local time a timer of `seconds` started at server time `started` ends.</summary>
    public static DateTime? Ends(double started, double seconds) => started > 0 ? At(started + seconds) : null;

    private static double? SecondsToNextGameDay => Get("next game day", () => Core.ServerUpdate.timeLeftForNextDay);

    /// <summary>The game's next daily reset, local time.</summary>
    public static DateTime? NextGameDay => SecondsToNextGameDay is { } left ? DateTime.Now.AddSeconds(left) : null;

    /// <summary>The game's next weekly reset, local time.</summary>
    public static DateTime? NextGameWeek => Get("next game week", () => Core.ServerUpdate.timeLeftForNextGameWeek) is { } left
        ? DateTime.Now.AddSeconds(left)
        : null;

    /// <summary>Whether a server timestamp falls before today's game day began (a daily claim not done today).</summary>
    public static bool? BeforeToday(double serverTime) =>
        ServerNow is { } now && SecondsToNextGameDay is { } left ? serverTime < now + left - 86400 : null;

    // ---- Empower. Matched the Temple's own texts on Steam-27 (26/11: found 10^211.73 vs 10^211.72 shown),
    // a few seconds late as the game recomputes it.

    public static double? FirestonesFoundLog10 => Get("firestones found", () =>
        Features.Character.Heroes.Log(H.Firestones.firestonesEarnedThisRun));

    public static double? FirestonesOwnedLog10 => Get("firestones owned", () =>
        Features.Character.Heroes.Log(H.BigCurrencyHandler.GetCurrency(BigCurrencyCode.Firestone).amount));

    public static TimeSpan? AdventureTime => Get("adventure time", () =>
        TimeSpan.FromSeconds(H.SpecialGameStatistics.save.timePlayedThisRun.GetDecrypted()));

    // ---- Timers, each checked on 26/11 against the bot's own Next read off the screen (within seconds).

    /// <summary>The earliest of some end times, null when none (or unreadable).</summary>
    private static DateTime? Earliest(string what, Func<List<DateTime>> ends) =>
        GetRef(what, ends) is { Count: > 0 } list ? list.Min() : null;

    /// <summary>When the first experiment in progress ends.</summary>
    public static DateTime? ExperimentsEnd => Earliest("experiments end", () =>
    {
        var ends = new List<DateTime>();
        foreach (var slot in H.AlchemyExperimentHandler.experimentSlot)
            if (slot.Value.ExperimentState == ExperimentState.InProgress && Ends(slot.Value.timeStarted, slot.Value.timeRequired) is { } end)
                ends.Add(end);
        return ends;
    });

    /// <summary>When the first Firestone research in progress ends; null too when a slot is free.</summary>
    public static DateTime? FirestoneResearchEnd => Earliest("firestone research end", () =>
    {
        var ends = new List<DateTime>();
        foreach (var slot in H.FirestoneResearchHandler.firestoneResearchSlots)
        {
            if (!slot.researchOnProgress || slot.firestoneResearch == null) return new List<DateTime>();
            if (Ends(slot.timeStarted, slot.firestoneResearch.realTimeRequired) is { } end) ends.Add(end);
        }

        return ends;
    });

    /// <summary>When the Engineer's tools are next ready (every WarMachineHandler.timeReq, 6 h).</summary>
    public static DateTime? EngineerToolsReady => Get("engineer tools", () => H.WarMachineHandler.lastClaimedTime) is { } last && last > 0
        ? At(last + WarMachineHandler.timeReq)
        : null;

    /// <summary>When the running Oracle ritual ends (OracleHandler.ritualDuration, 40 min).</summary>
    public static DateTime? RitualEnd => GetRef("oracle ritual", () => H.OracleHandler.activeRitual) is { } ritual &&
                                         Get("oracle ritual state", () => ritual.state == OracleRitual.State.Running) == true
        ? Ends(ritual.timeStarted, OracleHandler.ritualDuration)
        : null;

    // ---- Research. The data's names are keys ("cat1/mana_heroes_" for "Mana heroes", 27/11), so
    // nodes are matched by the preview popup's own research object, and named through KeyName.

    /// <summary>"cat1/mana_heroes_" -> "mana heroes": a research key as words, for the name-based priorities.</summary>
    public static string KeyName(string key) =>
        key == null ? "" : key[(key.LastIndexOf('/') + 1)..].Trim('_').Replace('_', ' ');

    /// <summary>The research the Firestone preview popup shows; null when it can't be read.</summary>
    public static FirestoneResearch FirestoneResearchPreviewed => GetRef("firestone research preview",
        () => GameInitialize.MenuLoader.FirestoneResearchPreviewPopup.firestoneResearch);

    public static MeteoriteResearch MeteoriteResearchPreviewed => GetRef("meteorite research preview",
        () => GameInitialize.MenuLoader.MeteoriteResearchPreviewPopup.meteoriteResearch);

    // ---- Meteorite research.

    public static double? Meteorites => Get("meteorites", () => H.CurrencyHandler.GetCurrency(CurrencyCode.Meteorite).Amount);

    /// <summary>What the Personal Tree (and War Machines) spend.</summary>
    public static double? ExpeditionTokens => Get("expedition tokens", () => H.CurrencyHandler.GetCurrency(CurrencyCode.ExpeditionToken).Amount);

    /// <summary>The cheapest level any unlocked, unmaxed meteorite research costs now; +Infinity when none.</summary>
    public static double? CheapestMeteoriteResearch => Get("meteorite research costs", () =>
    {
        var cheapest = double.PositiveInfinity;
        foreach (var tree in H.MeteoriteResearchHandler.researchTrees)
            foreach (var node in tree.Value)
                if (node.Value.IsUnlocked() && !node.Value.IsMaxLevel() && node.Value.levelUpCost.GetDecrypted() is var cost &&
                    cost > 0 && cost < cheapest)
                    cheapest = cost;
        return cheapest;
    });

    // ---- Events.

    /// <summary>
    ///     A fingerprint of the events' challenge state (calendar events and mini-events): equal means
    ///     nothing progressed or was claimed. StayOnline's progress is left out, as it moves every second.
    /// </summary>
    public static string EventsSignature => GetRef("events state", () =>
    {
        var sb = new System.Text.StringBuilder();
        var calendar = H.CalendarEventsHandler;
        foreach (var e in calendar.calendarEvents) sb.Append($"e{e.c}:{e.s}:{e.e}:{e.u};");
        foreach (var kv in calendar.eventChallengesProgress)
            sb.Append(kv.Key == CalendarEventChallenges.StayOnline ? $"c{kv.Key}:{kv.Value.c};" : $"c{kv.Key}:{kv.Value.p}:{kv.Value.c};");
        sb.Append($"t{calendar.totalTokensCollected};");
        foreach (var m in H.MiniEventsHandler.activeChallenges)
            sb.Append($"m{m.challengeCode}:{m.currentProgress}:{m.completed}:{m.claimed};");
        return sb.ToString();
    });

    // Which events run for this account, so the Events list isn't opened every hour to find out
    // (Steam-27, 27/11: three events locked or not listed, 7-8 s each, every hour).

    /// <summary>A mini-event on, and not locked by level.</summary>
    public static bool? MiniEventRunning => Get("mini event", () =>
        H.MiniEventsHandler.hasEvent && H.MiniEventsHandler.CurrentMiniEvent is { } e && !e.levelLocked);

    private static bool Within(CalendarEventsHandler.EventData e) =>
        e != null && ServerNow is { } now && now >= e.s && now <= e.e;

    /// <summary>
    ///     This calendar event is the current one or in the calendar's list, between its start and end.
    ///     27/11: Decorated Heroes ran with the current-event check alone saying no.
    /// </summary>
    public static bool? CalendarEventRunning(CalendarEvents code) => Get($"calendar event {code}", () =>
    {
        foreach (var e in H.CalendarEventsHandler.calendarEvents)
            if (e.c == (int)code && Within(e))
                return true;
        return H.CalendarEventsHandler.currentCalendarEvent is { } cur && cur.c == (int)code && Within(cur);
    });

    /// <summary>The calendar as the game holds it, for the log: code:start-end:u, server time first.</summary>
    public static string CalendarDescribe => GetRef("calendar describe", () =>
    {
        var h = H.CalendarEventsHandler;
        var cur = h.currentCalendarEvent;
        var list = new List<string>();
        foreach (var e in h.calendarEvents) list.Add($"{e.c}:{e.s:0}-{e.e:0}:{e.u}");
        return $"now {ServerNow:0}, current {(cur == null ? "-" : $"{cur.c}:{cur.s:0}-{cur.e:0}:{cur.u}")}, list {string.Join(" ", list)}";
    });

    /// <summary>The New Player (anniversary handler's) event between its start and end.</summary>
    public static bool? NewPlayerEventRunning => Get("new player event", () => Within(H.AnniversaryEventHandler.anniversaryEvent));

    // ---- Quests.

    /// <summary>Daily and weekly quests done and not claimed yet.</summary>
    public static int? ClaimableQuests => Get("quests claimable", () =>
    {
        var claimable = 0;
        foreach (var entry in H.QuestHandler.quests)
            if (!entry.Value.hasClaimed && entry.Value.questProgress >= entry.Value.questRequirement)
                claimable++;
        return claimable;
    });

    // ---- Currencies, by the game's code.

    public static double? Currency(CurrencyCode code) => Get($"currency {code}", () => H.CurrencyHandler.GetCurrency(code).Amount);

    // ---- Mail and Path of Glory.

    /// <summary>Mails with a reward not claimed yet.</summary>
    public static int? ClaimableMails => Get("system mail", () =>
    {
        var claimable = 0;
        foreach (var kv in H.SystemMailHandler.systemMails)
            if (!kv.Value.claimed && (kv.Value.rewards?.Count ?? 0) + (kv.Value.gameItems?.Count ?? 0) > 0)
                claimable++;
        return claimable;
    });

    public static int? BattlePassUnclaimed => Get("path of glory", () => H.BattlePassHandler.unclaimedRewards);

    // ---- Daily free claims: their handlers keep the last claim's server time.

    public static bool? OraclesGiftClaimable => Get("oracle's gift last claim", () => H.OraclesGiftDailyClaimHandler.lastClaim) is { } last
        ? BeforeToday(last)
        : null;

    // ---- Connection and activity.

    public static bool? Connected => Get("socket", () => Core.SocketAPI.IsConnectedAndNotReconnecting());

    /// <summary>The game is still busy with the last action: a menu loading, a transition, a chest opening.</summary>
    public static bool? Busy => Get("activity blocker", () => Core.PlayerActivityBlocker.IsActive());

    public static string MenuStack => GetRef("menu stack", () =>
    {
        var names = new List<string>();
        foreach (var id in Core.NavigationControl.menuStack) names.Add(((MenuName)id).ToString());
        return string.Join(">", names);
    });

    public static bool? IsMenuOpen(MenuName menu) => Get($"menu {menu}", () =>
    {
        foreach (var id in Core.NavigationControl.menuStack)
            if (id == (int)menu)
                return true;
        return false;
    });

    // ---- Badges. The rail's badge names (Paths.BattleLoc.NotificationsLoc) are NotificationType names,
    // three aside; on 26/11 HasNotification matched the lit rail badge for badge on Steam-27.

    private static readonly Dictionary<string, NotificationType> RenamedBadges = new()
    {
        ["OracleRituals"] = NotificationType.OracleRitual,
        ["OracleBlessings"] = NotificationType.OracleBlessing
    };

    public static NotificationType? BadgeType(string railName) =>
        RenamedBadges.TryGetValue(railName, out var renamed) ? renamed
        : Enum.TryParse<NotificationType>(railName, true, out var type) ? type
        : null;

    /// <summary>Null when the game can't say (not loaded yet, or a type it never registered).</summary>
    public static bool? HasBadge(NotificationType type) => Get($"badge {type}", () =>
        NotificationHandler.notifications.ContainsKey(type)
            ? H.NotificationHandler.HasNotification(type)
            : throw new KeyNotFoundException($"{type} not registered"));

    /// <summary>The badges the game has lit, by type name; null when it can't be read.</summary>
    public static string LitBadges => GetRef("lit badges", () =>
    {
        var lit = new List<string>();
        foreach (var kv in NotificationHandler.notifications)
            if (H.NotificationHandler.HasNotification(kv.Key))
                lit.Add(kv.Key.ToString());
        return string.Join(", ", lit.OrderBy(n => n, StringComparer.Ordinal));
    });

    // ---- Game build. A build can change the game's rules with no visible version change (06/11), so
    // the file's date and size count too.

    public static string Build => GetRef("game build", () =>
    {
        var file = new System.IO.FileInfo(System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "GameAssembly.dll"));
        return $"{BuildSettings.gameVersionText} ({BuildSettings.gameVersion}), GameAssembly.dll {file.Length} bytes {file.LastWriteTimeUtc:yyyy-MM-dd HH:mm}Z";
    });
}
