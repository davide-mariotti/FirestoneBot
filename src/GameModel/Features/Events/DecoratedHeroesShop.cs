using System;
using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

namespace Firebot.GameModel.Features.Events;

public static class DecoratedHeroesShop
{
    public static bool IsVisible => new GameElement(Paths.DecoratedHeroesShopLoc.Root).IsVisible();

    // Same reasoning as ChaosRiftShop.WaitUntilOpen: opening a sub-screen from a hub list plays a
    // transition that outlasts the standard interaction_delay - poll instead of guessing a duration.
    private static readonly WaitForSeconds OpenPollWait = new(0.5f);
    private const int MaxOpenPolls = 10;

    public static IEnumerator WaitUntilOpen()
    {
        var pollsLeft = MaxOpenPolls;
        while (pollsLeft > 0 && !IsVisible)
        {
            yield return OpenPollWait;
            pollsLeft--;
        }
    }

    public static IEnumerator Close => new GameButton(Paths.DecoratedHeroesShopLoc.CloseBtn).Click();

    public static IEnumerator OpenChallengesTab =>
        new GameButton(Paths.DecoratedHeroesShopLoc.ChallengesTabBtn).Click();

    public static IEnumerator OpenExchangeTab =>
        new GameButton(Paths.DecoratedHeroesShopLoc.ExchangeTabBtn).Click();

    /// <summary>
    ///     Claims every one of the (up to 8, per the user's screenshot) challenge cards that's
    ///     currently clickable - no comparison needed, every challenge is independently completable
    ///     and worth claiming regardless of the others, unlike Research's priority-vs-cheapest choice.
    /// </summary>
    public static IEnumerator ClaimAllChallenges()
    {
        foreach (var card in new GameElement(Paths.DecoratedHeroesShopLoc.ChallengeGridRoot).GetChildren())
        {
            var claimBtn = new GameButton(Paths.DecoratedHeroesShopLoc.ChallengeClaimBtn, card);
            if (claimBtn.IsClickable()) yield return claimBtn.Click();
        }
    }

    private static GameButton ExchangeQuantityBtn => new(Paths.DecoratedHeroesShopLoc.ExchangeQuantityBtn);

    private static GameText ExchangeQuantityTxt => new(Paths.DecoratedHeroesShopLoc.ExchangeQuantityTxt);

    private static readonly string[] QuantityCandidatesDescending = { "10", "5" };

    /// <summary>
    ///     Per the user's standing instruction (2026-09-23): always use a bulk-quantity multiplier
    ///     when one exists, same pattern as ChaosRift.TrySetBestQuantity - cycles looking for the
    ///     candidates highest-first, restores the original value if none of them turn up within one
    ///     full cycle. Exact cycle values not live-confirmed yet.
    /// </summary>
    public static IEnumerator TrySetBestQuantity()
    {
        if (QuantityCandidatesDescending.Any(c => ExchangeQuantityTxt.GetParsedText().Contains(c)))
            yield break;

        var original = ExchangeQuantityTxt.GetParsedText();
        var found = false;

        for (var i = 0; i < 6; i++)
        {
            yield return ExchangeQuantityBtn.Click();

            if (QuantityCandidatesDescending.Any(c => ExchangeQuantityTxt.GetParsedText().Contains(c)))
            {
                found = true;
                break;
            }

            if (ExchangeQuantityTxt.GetParsedText() == original) break; // full loop back - none exist
        }

        if (found) yield break;

        for (var i = 0; i < 6 && ExchangeQuantityTxt.GetParsedText() != original; i++)
            yield return ExchangeQuantityBtn.Click();
    }

    /// <summary>
    ///     Scans the Exchange list for an item whose name matches itemName (case-insensitive, partial
    ///     match) and buys it repeatedly until unaffordable/capped - the buy button's own IsClickable()
    ///     handles the "Claimed: X/50"-style cap seen in the user's screenshot, same safe-click
    ///     pattern used everywhere else in this codebase. Matched by name, not list position, so this
    ///     doesn't depend on the 16 items staying in the same order.
    /// </summary>
    public static IEnumerator BuyItem(string itemName, int maxBuys = 100)
    {
        foreach (var item in new GameElement(Paths.DecoratedHeroesShopLoc.ExchangeItemsRoot).GetChildren())
        {
            var name = new GameText(Paths.DecoratedHeroesShopLoc.ExchangeItemNameTxt, item).GetParsedText();
            if (!name.Contains(itemName, StringComparison.OrdinalIgnoreCase)) continue;

            var buyBtn = new GameButton(Paths.DecoratedHeroesShopLoc.ExchangeItemBuyBtn, item);
            for (var i = 0; i < maxBuys && buyBtn.IsClickable(); i++)
                yield return buyBtn.Click();

            yield break;
        }
    }
}
