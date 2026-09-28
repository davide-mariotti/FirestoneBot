using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild;

public static class ArcaneCrystal
{
    public static IEnumerator Hit => new GameButton(Paths.ArcaneCrystalLoc.HitBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.ArcaneCrystalLoc.CloseBtn).Click();

    private static GameText QuantityTxt => new(Paths.ArcaneCrystalLoc.QuantityTxt);

    private static bool IsFive(string quantityText) => quantityText.Contains("5");

    public static bool IsQuantitySetTo5 => IsFive(QuantityTxt.GetParsedText());

    /// <summary>
    ///     Tries to select an x5 hit multiplier so the Miner quest's 5 hits take one click. Whether x5
    ///     exists isn't known; check IsQuantitySetTo5 afterwards and fall back to single hits.
    /// </summary>
    public static IEnumerator TrySetQuantityTo5() =>
        QuantityToggle.CycleUntil(new GameButton(Paths.ArcaneCrystalLoc.ChangeQuantityBtn), QuantityTxt, IsFive);
}
