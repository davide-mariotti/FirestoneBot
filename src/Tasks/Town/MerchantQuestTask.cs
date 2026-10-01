using System;
using System.Collections;
using System.Linq;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;
using static Firebot.Core.BotSettings;
using Logger = Firebot.Core.Logger;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     The daily "Merchant" quest: sells as many items at the Exotic Merchant as the quest is missing,
///     then spends the coins on the first affordable Exotic Upgrade (only if priced in Exotic Coins). Only junk is ever sold (see
///     SellOrder, then FallbackOrder). The instant gold items are never sold nor used here.
/// </summary>
public class MerchantQuestTask : DailyQuestTask
{
    protected override int MinimumCharacterLevel => 30;

    protected override string QuestName => "Merchant";

    /// <summary>
    ///     Sold, by visible name and in this order: Midas' Touch first, then Health and Damage to top up.
    ///     Never Scroll of Speed - the F2P guide's main gold multiplier - nor anything not listed, like the
    ///     instant gold and meteorite consumables or a Barrel. An allowlist on purpose: a missing entry
    ///     costs one daily quest, while a blocklist missing a name could sell a Barrel.
    /// </summary>
    private static readonly string[] SellOrder = { "Midas' Touch", "Scroll of Health", "Scroll of Damage" };

    /// <summary>
    ///     The quest only, once SellOrder runs out: battle buffs the bot never uses, dozens on every
    ///     account. Chests don't refill SellOrder (01/10: 90 Wooden/Iron gave one Midas' Touch; scrolls
    ///     come from adventure missions), so 7 of 15 quests stopped at 3-9/10. Never Drums of War - it
    ///     boosts the instant gold items kept for manual use (the user's choice, 01/10).
    /// </summary>
    private static readonly string[] FallbackOrder =
        { "Totem of Annihilation", "Totem of Agony", "Guardian's Rune", "Dragon Armor" };

    protected override IEnumerator Work(int missing)
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenExoticMerchant;
        yield return ExoticMerchant.OpenSellItemsTab;

        var sold = 0;
        yield return SellJunk(missing, n => sold = n);
        if (sold < missing) yield return SellJunk(missing - sold, _ => { }, order: FallbackOrder);

        yield return ExoticMerchant.OpenUpgradesTab;
        yield return BuyUpgrades(1, _ => { }); // one upgrade per run

        yield return ExoticMerchant.Close;
        yield return TownScreen.Close;
    }

    // A bound on one challenge's sales, not a pacing choice: the upgrade's price is what stops them.
    private const int MaxSalesForUpgrade = 200;

    /// <summary>
    ///     An event challenge's exotic upgrades (EventChallengeActions): up to `count`, each the cheapest
    ///     one priced in Exotic Coins, whatever its priority. When the coins fall short, sells SellOrder
    ///     items - never anything else - until they cover it (the user's choice, 30/09).
    /// </summary>
    public static IEnumerator Upgrade(int count, Action<int> onBought)
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenExoticMerchant;

        var bought = 0;
        while (bought < count)
        {
            yield return ExoticMerchant.OpenUpgradesTab;
            var (name, cost) = ExoticMerchant.PricedUpgrades().OrderBy(u => u.Cost).FirstOrDefault();
            if (name == null)
            {
                Logger.Debug("[INFO] Exotic upgrade: none priced in Exotic Coins.");
                break;
            }

            var coins = ExoticMerchant.CoinCount;
            if (coins >= 0 && coins < cost)
            {
                yield return ExoticMerchant.OpenSellItemsTab;
                yield return SellJunk(MaxSalesForUpgrade, _ => { },
                    () => ExoticMerchant.CoinCount < 0 || ExoticMerchant.CoinCount >= cost);
                yield return ExoticMerchant.OpenUpgradesTab;
            }

            var before = ExoticMerchant.CoinCount;
            Logger.Debug($"[INFO] Exotic upgrade: cheapest '{name}' costs {cost}, coins {coins} -> {before} after sales.");
            if (before < cost) break;

            yield return new GameButton("/" + name + Paths.ExoticMerchantLoc.UpgradesLoc.UpgradeBtn,
                ExoticMerchant.UpgradesList).Click();
            yield return Poll.Until(() => ExoticMerchant.CoinCount < before);

            var after = ExoticMerchant.CoinCount;
            Logger.Debug($"[INFO] Exotic upgrade '{name}': coins {before} -> {after}.");
            if (after >= before) break;
            bought++;
        }

        onBought(bought);

        yield return ExoticMerchant.Close;
        yield return TownScreen.Close;
    }

    /// <summary>
    ///     The first affordable upgrades in list order, each only when priced in Exotic Coins and
    ///     counted once the coin counter drops.
    /// </summary>
    private static IEnumerator BuyUpgrades(int count, Action<int> onBought)
    {
        var bought = 0;
        foreach (var name in ExoticMerchant.UpgradesList.GetChildren().Select(c => c.Name).ToList())
        {
            if (bought >= count) break;
            if (string.IsNullOrEmpty(name)) continue;

            var upgradeBtn = new GameButton(
                "/" + name + Paths.ExoticMerchantLoc.UpgradesLoc.UpgradeBtn, ExoticMerchant.UpgradesList);
            if (!upgradeBtn.IsClickable()) continue;

            var icon = IconSprite.NameAt(
                $"{ExoticMerchant.UpgradesList.FullPath}/{name}{Paths.ExoticMerchantLoc.UpgradesLoc.UpgradeCostIcon}");
            if (icon != "exoticCoin64")
            {
                Logger.Debug($"[INFO] Exotic upgrade '{name}' is priced in '{icon}', not Exotic Coins - skipped.");
                continue;
            }

            var before = ExoticMerchant.CoinCount;
            yield return upgradeBtn.Click();
            yield return Poll.Until(() => ExoticMerchant.CoinCount < before);

            var after = ExoticMerchant.CoinCount;
            Logger.Debug($"[INFO] Exotic upgrade '{name}': coins {before} -> {after}.");
            if (after < before) bought++;
        }

        onBought(bought);
    }

    /// <summary>An event challenge's sales (EventChallengeActions): up to `count` of SellOrder only.</summary>
    public static IEnumerator Sell(int count, Action<int> onSold)
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenExoticMerchant;
        yield return ExoticMerchant.OpenSellItemsTab;

        yield return SellJunk(count, onSold);

        yield return ExoticMerchant.Close;
        yield return TownScreen.Close;
    }

    /// <summary>Up to `count` items of `order` (SellOrder by default), stopping early once enough() says so.</summary>
    private static IEnumerator SellJunk(int count, Action<int> onSold, Func<bool> enough = null, string[] order = null)
    {
        Logger.Debug("[INFO] Sell grid: " + string.Join(", ", ExoticMerchant.SellItems().Select(i => $"{i.Name} x{i.Quantity}")));

        var sold = 0;
        foreach (var name in order ?? SellOrder)
            while (sold < count && enough?.Invoke() != true && ExoticMerchant.TrySellOne(name))
            {
                sold++;
                yield return new WaitForSeconds(InteractionDelay);
            }

        Logger.Debug($"[INFO] Merchant: sold {sold}/{count}.");
        onSold(sold);
    }
}
