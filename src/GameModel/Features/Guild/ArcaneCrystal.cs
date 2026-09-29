using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild;

public static class ArcaneCrystal
{
    public static GameButton HitBtn => new(Paths.ArcaneCrystalLoc.HitBtn);

    /// <summary>
    ///     One hit, once the button is back: it's disabled while the previous hit animates, and a click
    ///     then is ignored (2 of 5 on Steam-16, 29/09). False when it never came back.
    /// </summary>
    public static IEnumerator Hit(System.Action<bool> onHit)
    {
        yield return Poll.Until(() => HitBtn.IsClickable(), 20, 0.3f);
        var ready = HitBtn.IsClickable();
        if (ready) yield return HitBtn.Click();
        onHit(ready);
    }

    public static IEnumerator Close => new GameButton(Paths.ArcaneCrystalLoc.CloseBtn).Click();

    public static int PickaxeCount => new GameText(Paths.ArcaneCrystalLoc.PickaxeCountTxt).GetParsedInt();

    /// <summary>Pickaxes one click spends: 1 means a single hit, one step of the Miner quest.</summary>
    public static int HitCost => new GameText(Paths.ArcaneCrystalLoc.HitCostTxt).GetParsedInt(-1);

    private static GameText QuantityTxt => new(Paths.ArcaneCrystalLoc.QuantityTxt);

    private static bool IsOne(string quantityText) => quantityText.Trim() == "x1";

    /// <summary>Selects x1 hits where the multiplier exists; a no-op where it's hidden.</summary>
    public static IEnumerator TrySetQuantityTo1() =>
        QuantityToggle.CycleUntil(new GameButton(Paths.ArcaneCrystalLoc.ChangeQuantityBtn), QuantityTxt, IsOne);
}
