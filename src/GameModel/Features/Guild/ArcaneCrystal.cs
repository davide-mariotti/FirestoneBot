using System.Collections;
using Firebot.Core;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild;

public static class ArcaneCrystal
{
    public static GameButton HitBtn => new(Paths.ArcaneCrystalLoc.HitBtn);

    /// <summary>
    ///     One hit, confirmed by the pickaxe counter going down. The button can look clickable and still
    ///     ignore a click, and an ignored click costs no pickaxe (30/09: 0 to 4 hits of 5 landed on 9
    ///     instances, with 80-96 pickaxes left), so a click that didn't spend one is repeated. False
    ///     when no click landed.
    /// </summary>
    public static IEnumerator Hit(System.Action<bool> onHit)
    {
        const int maxClicks = 3;
        var before = PickaxeCount;

        for (var click = 1; click <= maxClicks; click++)
        {
            yield return Poll.Until(() => HitBtn.IsClickable(), 20, 0.3f);
            if (!HitBtn.IsClickable()) break;

            yield return HitBtn.Click();
            yield return Poll.Until(() => PickaxeCount < before, 10, 0.3f);

            var after = PickaxeCount;
            Logger.Debug($"[ArcaneCrystal] click {click}/{maxClicks}: pickaxes {before} -> {after}.");
            if (after >= before) continue;

            onHit(true);
            yield break;
        }

        onHit(false);
    }

    public static IEnumerator Close => new GameButton(Paths.ArcaneCrystalLoc.CloseBtn).Click();

    // '.'-grouped past 999, like the other counters.
    public static int PickaxeCount => (int)new GameText(Paths.ArcaneCrystalLoc.PickaxeCountTxt).GetParsedDoubleAbbreviated();

    /// <summary>Pickaxes one click spends: 1 means a single hit, one step of the Miner quest.</summary>
    public static int HitCost => new GameText(Paths.ArcaneCrystalLoc.HitCostTxt).GetParsedInt(-1);

    private static GameText QuantityTxt => new(Paths.ArcaneCrystalLoc.QuantityTxt);

    private static bool IsOne(string quantityText) => quantityText.Trim() == "x1";

    /// <summary>Selects x1 hits where the multiplier exists; a no-op where it's hidden.</summary>
    public static IEnumerator TrySetQuantityTo1() =>
        QuantityToggle.CycleUntil(new GameButton(Paths.ArcaneCrystalLoc.ChangeQuantityBtn), QuantityTxt, IsOne);
}
