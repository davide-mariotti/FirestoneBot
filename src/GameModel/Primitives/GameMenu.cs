using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Shared;
using Il2Cpp;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     Opens screens with the game's own navigation instead of the clicks through the town or the
///     guild (GAMESTATE_PLAN.md, lever D). Three ways, tried in this order the first time a menu is
///     needed: the generic NavigationControl.OpenMenu, the HUD's AdventureScreen.Open*, the clicks.
///     The first that puts the menu on the game's stack is remembered for the session, so a way that
///     doesn't work for a menu costs its wait once (26/11: OpenMenu opened the Tavern, neither call
///     opened Magic Quarters, the HUD calls none).
/// </summary>
public static class GameMenu
{
    internal static AdventureScreen Adventure => GameInitialize.MenuLoader.AdventureScreen;

    private enum Way
    {
        Navigation,
        Hud,
        Clicks
    }

    // Neither game call opened these on Steam-27 (26-27/11, every session), so a restart doesn't pay
    // for finding that out again. The game's navigation did open the Tavern, Exotic Merchant, World
    // Map, Alchemist and Forbidden Knowledge.
    private static readonly Dictionary<MenuName, Way> Known = new()
    {
        [MenuName.MagicQuarters] = Way.Clicks,
        [MenuName.Library] = Way.Clicks,
        [MenuName.TownGuild] = Way.Clicks,
        [MenuName.Awakening] = Way.Clicks,
        [MenuName.PirateShip] = Way.Clicks
    };

    /// <summary>
    ///     Opens `menu` unless the game already has it open (a badge's shortcut often got there first):
    ///     the remembered way, else each way in turn. `hud` is the HUD's call for it (null if none).
    /// </summary>
    public static IEnumerator OpenOrClick(MenuName menu, Action hud, Func<IEnumerator> viaClicks)
    {
        if (GameState.IsMenuOpen(menu) == true) yield break;

        var known = Known.TryGetValue(menu, out var way);
        if (!known || way == Way.Navigation)
        {
            var opened = false;
            yield return Try(menu, "navigation", () => GameState.Core.NavigationControl.OpenMenu(menu, 0, 0, default), ok => opened = ok);
            if (opened)
            {
                Remember(menu, Way.Navigation);
                yield break;
            }
        }

        if (hud != null && (!known || way == Way.Hud))
        {
            var opened = false;
            yield return Try(menu, "HUD call", hud, ok => opened = ok);
            if (opened)
            {
                Remember(menu, Way.Hud);
                yield break;
            }
        }

        Remember(menu, Way.Clicks);
        yield return viaClicks();
    }

    private static void Remember(MenuName menu, Way way)
    {
        if (Known.TryGetValue(menu, out var before) && before == way) return;
        Known[menu] = way;
        Logger.Info($"[GameState] open {menu}: {way switch { Way.Navigation => "the game's navigation", Way.Hud => "the HUD call", _ => "clicks (no game call opened it)" }}.");
    }

    private static IEnumerator Try(MenuName menu, string label, Action open, Action<bool> done)
    {
        if (GameState.Get($"{label} {menu}", () =>
            {
                open();
                return true;
            }) != true)
        {
            done(false);
            yield break;
        }

        yield return Poll.AfterClick($"{label} {menu}");
        yield return Poll.Until(() => GameState.IsMenuOpen(menu) == true, 10, 0.1f);
        var opened = GameState.IsMenuOpen(menu) == true;
        if (!opened) Logger.Debug($"[GameState] open {menu} via {label} didn't show (stack [{GameState.MenuStack}]).");
        done(opened);
    }

    /// <summary>Clicks `open` only when the game doesn't already have `menu` open (or can't say).</summary>
    public static IEnumerator EnsureOpen(MenuName menu, Func<IEnumerator> open)
    {
        if (GameState.IsMenuOpen(menu) != true) yield return open();
    }
}
