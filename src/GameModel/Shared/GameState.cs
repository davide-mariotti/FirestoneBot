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

    // ---- Clock. Server timestamps are Unix seconds (probe, 26/11: serverTimeNow 7 s off the PC's UTC).

    public static double? ServerNow => Get("server clock", () =>
        Core.ServerUpdate is { gotServerTime: true } su ? su.serverTimeNow : throw new InvalidOperationException("no server time yet"));

    /// <summary>The local time at which a server timestamp falls.</summary>
    public static DateTime? At(double serverSeconds) =>
        ServerNow is { } now ? DateTime.Now.AddSeconds(serverSeconds - now) : null;

    /// <summary>The local time a timer of `seconds` started at server time `started` ends.</summary>
    public static DateTime? Ends(double started, double seconds) => started > 0 ? At(started + seconds) : null;

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
