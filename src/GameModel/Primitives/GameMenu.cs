using System;
using System.Collections;
using Firebot.GameModel.Shared;
using Il2Cpp;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     Opens screens with the game's own HUD functions (MenuLoader.AdventureScreen.Open*), the ones its
///     side buttons call: one call instead of the clicks through the town or the guild
///     (GAMESTATE_PLAN.md, lever D). The click path stays as the fallback.
/// </summary>
public static class GameMenu
{
    internal static AdventureScreen Adventure => GameInitialize.MenuLoader.AdventureScreen;

    /// <summary>
    ///     Opens `menu` with `open` unless the game already has it open (a badge's shortcut often got
    ///     there first), then waits for it on the menu stack. done(false) when the call failed or the
    ///     menu never showed: the caller then clicks its way there.
    /// </summary>
    public static IEnumerator Open(MenuName menu, Action open, Action<bool> done)
    {
        if (GameState.IsMenuOpen(menu) == true)
        {
            done(true);
            yield break;
        }

        if (GameState.Get($"open {menu}", () =>
            {
                open();
                return true;
            }) != true)
        {
            done(false);
            yield break;
        }

        yield return Poll.AfterClick($"open {menu}");
        yield return Poll.Until(() => GameState.IsMenuOpen(menu) == true, 10, 0.1f);

        // The HUD's call didn't take (26/11: MagicQuarters, Oracle, Expeditions, TownGuild): the game's
        // generic navigation next, with what the game says about why, once per menu.
        if (GameState.IsMenuOpen(menu) != true)
        {
            Diagnose(menu, "HUD call");
            if (GameState.Get($"navigate {menu}", () =>
                {
                    GameState.Core.NavigationControl.OpenMenu(menu, 0, 0, default);
                    return true;
                }) == true)
            {
                yield return Poll.AfterClick($"navigate {menu}");
                yield return Poll.Until(() => GameState.IsMenuOpen(menu) == true, 10, 0.1f);
            }
        }

        var opened = GameState.IsMenuOpen(menu) == true;
        if (!opened)
        {
            Diagnose(menu, "navigation");
            Logger.Info($"[GameState] fallback open {menu}: not on the menu stack after the calls ({GameState.MenuStack}).");
        }

        done(opened);
    }

    private static readonly System.Collections.Generic.HashSet<string> Diagnosed = new();

    private static void Diagnose(MenuName menu, string attempt)
    {
        if (!Diagnosed.Add($"{menu} {attempt}")) return;
        var state = GameState.GetRef("navigation state", () =>
        {
            var nav = GameState.Core.NavigationControl;
            var visible = new Base.GameElement($"{Infrastructure.Paths.WatchdogLoc.MenusRoot}/{menu}").IsVisible() ||
                          new Base.GameElement($"{Infrastructure.Paths.WatchdogLoc.PopupsRoot}/{menu}").IsVisible();
            return $"allowNavigation={nav.allowNavigation} pending={nav.pendingChangeMenu} stack=[{GameState.MenuStack}] " +
                   $"menu visible={visible} adventureScreen={(Adventure == null ? "null" : "set")} blocker={GameState.Busy}";
        });
        Logger.Info($"[GameState] open {menu} via {attempt} didn't show: {state}");
    }

    /// <summary>Open(menu, open), and the click path when that fails.</summary>
    public static IEnumerator OpenOrClick(MenuName menu, Action open, Func<IEnumerator> viaClicks)
    {
        var opened = false;
        yield return Open(menu, open, ok => opened = ok);
        if (!opened) yield return viaClicks();
    }

    /// <summary>Clicks `open` only when the game doesn't already have `menu` open (or can't say).</summary>
    public static IEnumerator EnsureOpen(MenuName menu, Func<IEnumerator> open)
    {
        if (GameState.IsMenuOpen(menu) != true) yield return open();
    }
}
