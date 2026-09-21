using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

namespace Firebot.GameModel.Features.Guild;

public static class Awakening
{
    // Highest first - SelectBestMultiplier tries these in order and stops at the first one that's
    // actually clickable (unlocked and, per the wiki, enough crystals banked for at least one use).
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

    // Awaken plays a full spine animation (crystals flying in, hero glow) that runs well past the
    // standard interaction_delay - found via live testing: the task moved on (re-selecting the
    // multiplier / clicking again) before the animation resolved, which visibly cancelled it instead
    // of actually spending the crystals. This is a rough estimate, not measured frame-by-frame -
    // adjust if it's still cutting the animation short or needlessly slow once tested live.
    private static readonly WaitForSeconds AwakenAnimationWait = new(2.5f);

    public static GameButton AwakenBtn => new(Paths.AwakeningLoc.AwakenBtn);

    /// <summary>Selects the biggest multiplier currently usable - x1 is always available per the
    /// wiki, so this always selects something (never a no-op).</summary>
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

    /// <summary>Clicks AwakenBtn and waits out its result animation before returning, so the next
    /// loop iteration doesn't interrupt it - see AwakenAnimationWait.</summary>
    public static IEnumerator Awaken()
    {
        yield return AwakenBtn.Click();
        yield return AwakenAnimationWait;
    }

    public static IEnumerator Close => new GameButton(Paths.AwakeningLoc.CloseBtn).Click();
}
