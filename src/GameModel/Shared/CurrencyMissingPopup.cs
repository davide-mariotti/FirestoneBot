using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Shared;

/// <summary>
///     "You need N more ..." - several spend buttons stay clickable even when unaffordable, so this
///     popup is the only reliable signal that a spend loop has run out and must stop.
/// </summary>
public static class CurrencyMissingPopup
{
    public static bool IsShowing => new GameElement(Paths.MenusLoc.CurrencyMissingLoc.CloseBtn).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.CurrencyMissingLoc.CloseBtn).Click();
}
