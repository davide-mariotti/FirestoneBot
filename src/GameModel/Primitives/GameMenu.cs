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
        yield return Poll.Until(() => GameState.IsMenuOpen(menu) == true, 20, 0.1f);

        var opened = GameState.IsMenuOpen(menu) == true;
        if (!opened) Logger.Info($"[GameState] fallback open {menu}: not on the menu stack after the call ({GameState.MenuStack}).");
        done(opened);
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
