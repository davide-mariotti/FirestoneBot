using System;
using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using UnityEngine;
using Logger = Firebot.Core.Logger;

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

    // Per the user (2026-09-24): each of the 8 challenge cards can be claimed up to 3 times (3 reward
    // tiers per challenge, not just one) - re-checks IsClickable() between attempts so this is a safe
    // no-op once a card runs out of tiers, without needing to know its exact tier count up front.
    private const int MaxClaimsPerChallenge = 3;

    /// <summary>
    ///     Claims every one of the (up to 8, per the user's screenshot) challenge cards, up to
    ///     MaxClaimsPerChallenge times each - no comparison needed, every challenge is independently
    ///     completable and worth claiming regardless of the others, unlike Research's
    ///     priority-vs-cheapest choice.
    /// </summary>
    public static IEnumerator ClaimAllChallenges()
    {
        var cards = new GameElement(Paths.DecoratedHeroesShopLoc.ChallengeGridRoot).GetChildren().ToList();
        Logger.Debug($"[DecoratedHeroesShop] ClaimAllChallenges: {cards.Count} card(s) under ChallengeGridRoot.");

        var claimed = 0;
        foreach (var card in cards)
        {
            var claimBtn = new GameButton(Paths.DecoratedHeroesShopLoc.ChallengeClaimBtn, card);
            for (var i = 0; i < MaxClaimsPerChallenge && claimBtn.IsClickable(); i++)
            {
                yield return claimBtn.Click();
                claimed++;
            }
        }

        Logger.Debug($"[DecoratedHeroesShop] ClaimAllChallenges: claimed {claimed} time(s) across {cards.Count} card(s).");
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
        var startText = ExchangeQuantityTxt.GetParsedText();
        Logger.Debug($"[DecoratedHeroesShop] TrySetBestQuantity: current quantity text = '{startText}'.");

        if (QuantityCandidatesDescending.Any(c => startText.Contains(c)))
            yield break;

        var original = startText;
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

        Logger.Debug($"[DecoratedHeroesShop] TrySetBestQuantity: found={found}, ended at '{ExchangeQuantityTxt.GetParsedText()}'.");

        if (found) yield break;

        for (var i = 0; i < 6 && ExchangeQuantityTxt.GetParsedText() != original; i++)
            yield return ExchangeQuantityBtn.Click();
    }

    /// <summary>
    ///     Scans the Exchange list for an item whose name matches itemName (case-insensitive, partial
    ///     match) and buys it repeatedly until unaffordable/capped. Live-confirmed, 2026-09-24 (user
    ///     screenshot: Golden key costs 1.000 Stars of Recognition, only 280 on hand, Claimed 5/50):
    ///     the buy button's own IsClickable() does NOT reflect real affordability here - same
    ///     "spend action's button stays clickable regardless of balance" shape CurrencyMissingPopup
    ///     already exists for (Tree of Life, War Machines). Every click here now checks for that popup
    ///     and stops immediately once it appears, instead of blindly hammering an unaffordable button
    ///     up to maxBuys times. Matched by name, not list position, so this doesn't depend on the 16
    ///     items staying in the same order.
    /// </summary>
    public static IEnumerator BuyItem(string itemName, int maxBuys = 100)
    {
        var items = new GameElement(Paths.DecoratedHeroesShopLoc.ExchangeItemsRoot).GetChildren().ToList();
        var names = string.Join(", ", items.Select(i =>
            new GameText(Paths.DecoratedHeroesShopLoc.ExchangeItemNameTxt, i).GetParsedText()));
        Logger.Debug($"[DecoratedHeroesShop] BuyItem('{itemName}'): {items.Count} item(s) scanned: {names}");

        foreach (var item in items)
        {
            var name = new GameText(Paths.DecoratedHeroesShopLoc.ExchangeItemNameTxt, item).GetParsedText();
            if (!name.Contains(itemName, StringComparison.OrdinalIgnoreCase)) continue;

            var buyBtn = new GameButton(Paths.DecoratedHeroesShopLoc.ExchangeItemBuyBtn, item);
            var bought = 0;
            for (var i = 0; i < maxBuys && buyBtn.IsClickable(); i++)
            {
                yield return buyBtn.Click();

                if (CurrencyMissingPopup.IsShowing)
                {
                    Logger.Debug($"[DecoratedHeroesShop] BuyItem: currency ran out after {bought} real purchase(s) - this click didn't go through.");
                    yield return CurrencyMissingPopup.Close;
                    break;
                }

                bought++;
            }

            Logger.Debug($"[DecoratedHeroesShop] BuyItem: matched '{name}', bought {bought}x.");
            yield break;
        }

        Logger.Debug($"[DecoratedHeroesShop] BuyItem: no item matching '{itemName}' found.");
    }
}
