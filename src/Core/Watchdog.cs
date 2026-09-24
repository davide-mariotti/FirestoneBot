using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

namespace Firebot.Core;

/// <summary>
///     Generic safety net, not feature-specific: scans for any leftover popup/event/menu with a
///     close or collect button visible and closes it. Run by BotManager before and after every
///     scheduled task, so a stray popup (a level-up celebration, an unclosed event, anything left
///     over from a manual play session) can't block or misdirect the task's own clicks.
/// </summary>
public static class Watchdog
{
    private static IEnumerable<string> EnumerateNuisancePaths()
    {
        foreach (var path in EnumerateChildPaths(new GameElement(Paths.WatchdogLoc.EventsRoot),
                     Paths.WatchdogLoc.EventsRoot,
                     Paths.WatchdogLoc.CloseSuffix,
                     "bg/closeButton"))
            yield return path;

        foreach (var path in EnumerateChildPaths(new GameElement(Paths.WatchdogLoc.PopupsRoot),
                     Paths.WatchdogLoc.PopupsRoot,
                     Paths.WatchdogLoc.CloseSuffix,
                     "bg/closeButton"))
            yield return path;

        foreach (var path in EnumerateChildPaths(new GameElement(Paths.WatchdogLoc.PopupsRoot),
                     Paths.WatchdogLoc.PopupsRoot,
                     Paths.WatchdogLoc.CollectSuffix,
                     "bg/collectButton"))
            yield return path;

        foreach (var path in EnumerateChildPaths(new GameElement(Paths.WatchdogLoc.MenusRoot),
                     Paths.WatchdogLoc.MenusRoot,
                     Paths.WatchdogLoc.MenuCloseSuffix,
                     "closeButton"))
            yield return path;
    }

    private static IEnumerable<string> EnumerateChildPaths(GameElement rootElement, string basePath, string suffix,
        string probePath)
    {
        foreach (var child in rootElement.GetChildren())
        {
            var probe = new GameElement(probePath, child);
            if (probe.IsVisible())
                yield return $"{basePath}/{child.Name}{suffix}";
        }
    }

    public static IEnumerator ForceClearAll()
    {
        for (var i = 0; i < 3; i++)
            foreach (var path in EnumerateNuisancePaths())
            {
                var gameButton = new GameButton(path);

                if (!gameButton.IsVisible()) continue;

                Debug.Log($"[Watchdog] Closing popup: {path}");
                yield return gameButton.Click();
            }
    }

    /// <summary>
    ///     Read-only diagnostic (no clicking): lists every currently-active real child under the same
    ///     three roots ForceClearAll knows about (events/, popups/, menus/) - i.e. "what screen is
    ///     actually showing right now, by its real name". Written 3 times across live investigations
    ///     this session (EventManager, WarfrontDailyMissionsTask) before being promoted here - the
    ///     go-to first step whenever a screen isn't where the code expects and the real name/location
    ///     needs discovering from scratch (same live-diagnostic approach used throughout this
    ///     codebase's history, see e.g. ChaosRiftLoc/PirateShipLoc's own doc comments).
    /// </summary>
    public static string DumpActiveScreens()
    {
        var events = new GameElement(Paths.WatchdogLoc.EventsRoot).GetChildren()
            .Where(e => e.IsVisible()).Select(e => $"events/{e.Name}");
        var popups = new GameElement(Paths.WatchdogLoc.PopupsRoot).GetChildren()
            .Where(p => p.IsVisible()).Select(p => $"popups/{p.Name}");
        var menus = new GameElement(Paths.WatchdogLoc.MenusRoot).GetChildren()
            .Where(m => m.IsVisible()).Select(m => $"menus/{m.Name}");

        var all = events.Concat(popups).Concat(menus).ToList();
        return all.Count == 0 ? "(none active)" : string.Join(", ", all);
    }

    /// <summary>
    ///     Read-only diagnostic (no clicking): walks every currently-active descendant of rootPath up
    ///     to maxDepth and returns one "path (active)" line per node - the standard next step once
    ///     DumpActiveScreens (or any other lead) points at a real but previously-unmapped screen, and
    ///     its actual internal layout (tabs, claim buttons, item lists) needs discovering from scratch.
    ///     Written ad hoc for EventManager's own investigation earlier this session before being
    ///     promoted here for reuse.
    /// </summary>
    public static string DumpChildrenRecursive(string rootPath, int maxDepth = 4)
    {
        var lines = new List<string>();
        Walk(new GameElement(rootPath), rootPath, 0, maxDepth, lines);
        return lines.Count == 0 ? "(no children)" : string.Join("\n", lines);
    }

    private static void Walk(GameElement element, string path, int depth, int maxDepth, List<string> lines)
    {
        if (depth > maxDepth) return;

        foreach (var child in element.GetChildren())
        {
            var childPath = $"{path}/{child.Name}";
            var visible = child.IsVisible();
            lines.Add($"{childPath} (active={visible})");
            if (visible) Walk(child, childPath, depth + 1, maxDepth, lines);
        }
    }
}
