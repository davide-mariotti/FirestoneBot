using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.Core;

/// <summary>
///     Safety net run before and after every task: closes any leftover popup, event or menu that
///     has a visible close/collect button - a level-up celebration, a promo, an unclosed screen -
///     so it can't swallow or misdirect the task's own clicks.
/// </summary>
public static class Watchdog
{
    private static readonly (string Root, string ButtonSuffix)[] Sweeps =
    {
        (Paths.WatchdogLoc.EventsRoot, Paths.WatchdogLoc.CloseSuffix),
        (Paths.WatchdogLoc.PopupsRoot, Paths.WatchdogLoc.CloseSuffix),
        (Paths.WatchdogLoc.PopupsRoot, Paths.WatchdogLoc.CollectSuffix),
        (Paths.WatchdogLoc.MenusRoot, Paths.WatchdogLoc.MenuCloseSuffix)
    };

    public static IEnumerator ForceClearAll()
    {
        for (var pass = 0; pass < 3; pass++)
            foreach (var (root, buttonSuffix) in Sweeps)
                foreach (var screen in new GameElement(root).GetChildren())
                {
                    var button = new GameButton($"{root}/{screen.Name}{buttonSuffix}");
                    if (!button.IsVisible()) continue;

                    Logger.Debug($"[Watchdog] Closing popup: {button.FullPath}");
                    yield return button.Click();
                }
    }

    /// <summary>Read-only: every screen active right now under events/, popups/ and menus/.</summary>
    public static string DumpActiveScreens()
    {
        var all = new[]
            {
                (Label: "events", Root: Paths.WatchdogLoc.EventsRoot),
                (Label: "popups", Root: Paths.WatchdogLoc.PopupsRoot),
                (Label: "menus", Root: Paths.WatchdogLoc.MenusRoot)
            }
            .SelectMany(r => new GameElement(r.Root).GetChildren()
                .Where(screen => screen.IsVisible()).Select(screen => $"{r.Label}/{screen.Name}"))
            .ToList();
        return all.Count == 0 ? "(none active)" : string.Join(", ", all);
    }

    /// <summary>Read-only: one "path (active=...)" line per descendant, down to maxDepth.</summary>
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
