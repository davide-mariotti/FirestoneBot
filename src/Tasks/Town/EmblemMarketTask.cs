using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Base;
using Firebot.GameModel.Features.Inventory;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using UnityEngine;
using InventoryScreen = Firebot.GameModel.Features.Inventory.Inventory;
using TownScreen = Firebot.GameModel.Features.Town.Town;
using Loc = Firebot.Infrastructure.Paths.ExoticMerchantLoc.EmblemMarketLoc;

namespace Firebot.Tasks.Town;

/// <summary>
///     Exotic Merchant's Emblem market: in gear (Emblems of Courage) and jewels (Emblems of Valor), as
///     many lots of the rarest chest on offer as the emblems pay for, then opens exactly the chests
///     bought. Never a cheaper chest instead, never celestials (the user's rules, 01/10). Emblems buy
///     nothing else, so no reserve is kept.
/// </summary>
public class EmblemMarketTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    protected override int MinimumCharacterLevel => 65;

    // Emblems come from missions and the campaign; a lot costs 5,000.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // The inventory's chest slots are created a moment after the tab opens (CollectorQuestTask).
    private static readonly WaitForSeconds ChestListPopulateDelay = new(1.5f);

    // A safety bound on one category's purchases, not a pacing choice: the emblems are what stop it.
    private const int MaxLotsPerRun = 20;

    public override IEnumerator Execute()
    {
        NextRunTime = DateTime.Now + RecheckDelay;

        yield return TownScreen.Open;
        yield return TownScreen.OpenExoticMerchant;

        // The merchant slides in: its tabs were still hidden 1 s after the click (Steam-0, 01/10).
        var tab = new GameButton(Loc.TabBtn);
        yield return Poll.Until(() => GameElement.FindTransform(Loc.TabBtn)?.gameObject.activeInHierarchy == true);

        var bought = new List<(string Slot, int Count)>();
        if (GameElement.FindTransform(Loc.TabLock)?.gameObject.activeInHierarchy != false || !tab.IsClickable())
            Debug("[INFO] Emblem market: tab locked or not shown - next check in 6 h.");
        else
        {
            yield return ExoticMerchant.OpenEmblemMarketTab;
            yield return BuyRarest("gear", Loc.GearCategoryBtn, Loc.GearGrid, Loc.CourageCountTxt, "emblemOfCourage64", bought);
            yield return BuyRarest("jewels", Loc.JewelsCategoryBtn, Loc.JewelsGrid, Loc.ValorCountTxt, "emblemOfValor64", bought);
        }

        yield return ExoticMerchant.Close;
        yield return TownScreen.Close;

        if (bought.Count > 0) yield return OpenBought(bought);
    }

    /// <summary>
    ///     Lots of the category's rarest visible chest, each only when priced in `currency` and covered
    ///     by the counter, and counted once the counter drops by exactly its cost.
    /// </summary>
    private IEnumerator BuyRarest(string category, string categoryBtn, string grid, string counterPath, string currency,
        List<(string Slot, int Count)> bought)
    {
        yield return new GameButton(categoryBtn).Click();
        yield return Poll.Until(() => ExoticMerchant.EmblemOffers(grid).Count > 0);

        var counter = new GameText(counterPath);
        int Emblems() => (int)counter.GetParsedDoubleAbbreviated(-1);

        var offer = Rarest(grid);
        if (offer == null)
        {
            Debug($"[INFO] Emblem market {category}: no chest shown.");
            yield break;
        }

        var start = Emblems();
        var emblems = start;
        var lots = 0;
        for (; lots < MaxLotsPerRun; lots++)
        {
            // Read again before every click, so a cell that changed is never paid by mistake.
            var now = Rarest(grid);
            if (now == null || now.Rarity != offer.Rarity) break;
            if (now.Currency != currency)
            {
                Debug($"[INFO] Emblem market {category}: '{now.Name}' is priced in '{now.Currency}', not {currency} - skipped.");
                break;
            }

            if (now.Cost <= 0 || emblems < now.Cost) break;

            // The game re-enables buying a moment after each purchase (reenableEmblemMarketBuyTime).
            var buy = new GameButton(transform: now.BuyButton);
            yield return Poll.Until(buy.IsClickable, 20);
            yield return buy.Click();

            var before = emblems;
            yield return Poll.Until(() => Emblems() == before - now.Cost, 20);
            emblems = Emblems();
            if (emblems != before - now.Cost)
            {
                Debug($"[FAILED] Emblem market {category}: '{now.Name}' clicked for {now.Cost}, emblems {before} -> " +
                      $"{emblems}. Screens: {Watchdog.DumpActiveScreens()}");
                break;
            }
        }

        Debug($"[INFO] Emblem market {category}: '{offer.Name}' (rarity {offer.Rarity} {offer.Chest}), {offer.Cost} each, " +
              $"emblems {start} -> {emblems}, {lots} lot(s).");
        if (lots > 0) bought.Add(("/" + offer.Chest, lots * offer.Quantity));
    }

    private static ExoticMerchant.EmblemOffer Rarest(string grid) =>
        ExoticMerchant.EmblemOffers(grid).OrderByDescending(o => o.Rarity).FirstOrDefault();

    private IEnumerator OpenBought(List<(string Slot, int Count)> bought)
    {
        yield return InventoryScreen.Open;
        yield return InventoryScreen.OpenChestsTab;
        yield return ChestListPopulateDelay;

        foreach (var (slot, count) in bought)
        {
            var opened = 0;
            yield return ChestOpening.OpenDownTo(slot, 0, n => opened = n, count);
            Debug($"[INFO] Emblem market: opened {opened}/{count} '{slot}'.");
        }

        yield return InventoryScreen.Close;
    }
}
