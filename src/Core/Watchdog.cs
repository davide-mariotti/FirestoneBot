using System.Collections;
using System.Collections.Generic;
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
}
