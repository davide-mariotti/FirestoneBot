using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

namespace Firebot.GameModel.Features.Guild;

public static class Awakening
{
    private static readonly string[] QuantityButtonsDescending =
    {
        Paths.AwakeningLoc.QuantityBtn160,
        Paths.AwakeningLoc.QuantityBtn80,
        Paths.AwakeningLoc.QuantityBtn40,
        Paths.AwakeningLoc.QuantityBtn20,
        Paths.AwakeningLoc.QuantityBtn10,
        Paths.AwakeningLoc.QuantityBtn5,
        Paths.AwakeningLoc.QuantityBtn2,
        Paths.AwakeningLoc.QuantityBtn1
    };

    // Awaken plays an animation that the next click would cancel before the crystals are spent.
    // An estimate, not measured: tune it if awakenings still get cut short, or if it's needlessly slow.
    private static readonly WaitForSeconds AwakenAnimationWait = new(2.5f);

    public static GameButton AwakenBtn => new(Paths.AwakeningLoc.AwakenBtn);

    /// <summary>Selects the biggest multiplier currently available (x1 always is).</summary>
    public static IEnumerator SelectBestMultiplier()
    {
        foreach (var path in QuantityButtonsDescending)
        {
            var button = new GameButton(path);
            if (!button.IsClickable()) continue;

            yield return button.Click();
            yield break;
        }
    }

    public static IEnumerator Awaken()
    {
        yield return AwakenBtn.Click();
        yield return AwakenAnimationWait;
    }

    public static IEnumerator Close => new GameButton(Paths.AwakeningLoc.CloseBtn).Click();
}
