using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

namespace Firebot.GameModel.Features.Town;

public static class Tavern
{
    // The card-flip has its own reveal animation (variable length, longer for a bigger batch) -
    // poll for the next step's button to actually become clickable instead of guessing a fixed
    // delay (same lesson as ChestOpening.ChestTransitionPollWait).
    private static readonly WaitForSeconds AnimationPollWait = new(0.3f);
    private const int MaxAnimationPolls = 25; // ~7.5s ceiling

    public static IEnumerator OpenMarket => new GameButton(Paths.MenusLoc.TavernLoc.OpenMarketBtn).Click();

    public static GameButton PlayBtn => new(Paths.MenusLoc.TavernLoc.PlayBtn);

    private static GameButton FirstCardBtn => new(Paths.MenusLoc.TavernLoc.FirstCardBtn);

    /// <summary>
    ///     Clicks Play at whatever quantity is currently set, then picks a card to actually trigger
    ///     the reveal and complete the round - live-confirmed, 2026-09-18: Play alone doesn't deduct
    ///     tokens or count towards the quest, it just reveals a set of interchangeable face-down card
    ///     stacks (see Paths.MenusLoc.TavernLoc.CardsRoot) that need a follow-up click. Any one card
    ///     works - same bundled reward regardless of which is picked.
    /// </summary>
    public static IEnumerator PlayRound()
    {
        yield return PlayBtn.Click();

        var pollsLeft = MaxAnimationPolls;
        while (pollsLeft > 0 && !FirstCardBtn.IsClickable())
        {
            yield return AnimationPollWait;
            pollsLeft--;
        }

        yield return FirstCardBtn.Click();

        pollsLeft = MaxAnimationPolls;
        while (pollsLeft > 0 && !PlayBtn.IsClickable())
        {
            yield return AnimationPollWait;
            pollsLeft--;
        }
    }

    public static int GameTokenCount => new GameText(Paths.MenusLoc.TavernLoc.GameTokenCountTxt).GetParsedInt();

    private static GameText PlayQuantityTxt => new(Paths.MenusLoc.TavernLoc.QuantityTxt);

    private static GameButton ChangePlayQuantityBtn => new(Paths.MenusLoc.TavernLoc.ChangeQuantityBtn);

    private static int ParsedPlayQuantity =>
        int.TryParse(PlayQuantityTxt.GetParsedText().TrimStart('x', 'X').Trim(), out var n) ? n : -1;

    public static bool IsPlayQuantitySetTo(int quantity) => ParsedPlayQuantity == quantity;

    /// <summary>
    ///     User-requested optimization for GamerQuestTask (needs exactly 10 draws/day): tries cycling
    ///     changeQuantity to find an exact "quantity" multiplier so one Play click does several draws
    ///     at once instead of one at a time - same pattern as ArcaneCrystal.TrySetQuantityTo5. If
    ///     "quantity" is never found within one full cycle, restores the original multiplier exactly
    ///     before returning, so a caller falling back to individual Play clicks isn't left at some
    ///     other multiplier by mistake. Check IsPlayQuantitySetTo(quantity) afterward to know which
    ///     case happened.
    /// </summary>
    public static IEnumerator TrySetPlayQuantityTo(int quantity)
    {
        if (IsPlayQuantitySetTo(quantity)) yield break;

        var original = PlayQuantityTxt.GetParsedText();
        var found = false;

        for (var i = 0; i < 6; i++)
        {
            yield return ChangePlayQuantityBtn.Click();

            if (IsPlayQuantitySetTo(quantity))
            {
                found = true;
                break;
            }

            if (PlayQuantityTxt.GetParsedText() == original) break; // full loop back - not an option
        }

        if (found) yield break;

        for (var i = 0; i < 6 && PlayQuantityTxt.GetParsedText() != original; i++)
            yield return ChangePlayQuantityBtn.Click();
    }

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.TavernLoc.CloseBtn).Click();
}
