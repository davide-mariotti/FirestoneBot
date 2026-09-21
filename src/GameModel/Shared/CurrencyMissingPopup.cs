using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Shared;

/// <summary>
///     Generic "You need N more &lt;currency&gt;..." blocking popup ("CurrencyMissing") - shows up
///     whenever a spend action's own button stays clickable regardless of real affordability. First
///     confirmed on Tree of Life's personal upgrades, then again on War Machines' level-up (both
///     spend Expedition Tokens). Any loop driving one of these actions should check this after every
///     attempt and stop immediately rather than looping uselessly against a wall it can't get past
///     (per the user, 2026-09-18).
/// </summary>
public static class CurrencyMissingPopup
{
    public static bool IsShowing => new GameElement(Paths.MenusLoc.CurrencyMissingLoc.CloseBtn).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.CurrencyMissingLoc.CloseBtn).Click();
}
