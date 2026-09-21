using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild;

public static class ArcaneCrystal
{
    public static IEnumerator Hit => new GameButton(Paths.ArcaneCrystalLoc.HitBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.ArcaneCrystalLoc.CloseBtn).Click();

    private static GameText QuantityTxt => new(Paths.ArcaneCrystalLoc.QuantityTxt);

    private static GameButton ChangeQuantityBtn => new(Paths.ArcaneCrystalLoc.ChangeQuantityBtn);

    /// <summary>Whether the hit-quantity multiplier currently reads as 5 (e.g. "x5").</summary>
    public static bool IsQuantitySetTo5 => QuantityTxt.GetParsedText().Contains("5");

    /// <summary>
    ///     User-requested optimization for MinerQuestTask (needs exactly 5 hits): tries cycling
    ///     changeHitQuantity to find a "5" multiplier so one Hit click does all 5 at once instead of
    ///     5 separate clicks. The available multipliers aren't live-confirmed (static scan guesses a
    ///     common x1/x10/x100 idle-RPG pattern) - if "5" is never found within one full cycle, this
    ///     restores the original quantity exactly before returning, so a caller that falls back to 5
    ///     individual Hit clicks isn't left hitting at some other multiplier by mistake. Check
    ///     IsQuantitySetTo5 afterward to know which case happened.
    /// </summary>
    public static IEnumerator TrySetQuantityTo5()
    {
        if (IsQuantitySetTo5) yield break;

        var original = QuantityTxt.GetParsedText();
        var found = false;

        for (var i = 0; i < 6; i++)
        {
            yield return ChangeQuantityBtn.Click();

            if (IsQuantitySetTo5)
            {
                found = true;
                break;
            }

            if (QuantityTxt.GetParsedText() == original) break; // full loop back - "5" isn't an option
        }

        if (found) yield break;

        for (var i = 0; i < 6 && QuantityTxt.GetParsedText() != original; i++)
            yield return ChangeQuantityBtn.Click();
    }
}
