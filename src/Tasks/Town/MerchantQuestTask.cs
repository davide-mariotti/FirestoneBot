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
///     SellOrder); if it runs out, the quest waits for more (CollectorQuestTask opens extra chests
///     for it). The instant gold items are never sold nor used here.
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

    protected override IEnumerator Work(int missing)
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenExoticMerchant;
        yield return ExoticMerchant.OpenSellItemsTab;

        yield return SellJunk(missing, _ => { });

        yield return ExoticMerchant.OpenUpgradesTab;
        yield return BuyUpgrades(1, _ => { }); // one upgrade per run

        yield return ExoticMerchant.Close;
        yield return TownScreen.Close;
    }

    /// <summary>An event challenge's exotic upgrades (EventChallengeActions): up to `count`.</summary>
    public static IEnumerator Upgrade(int count, Action<int> onBought)
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenExoticMerchant;
        yield return ExoticMerchant.OpenUpgradesTab;

        yield return BuyUpgrades(count, onBought);

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

    private static IEnumerator SellJunk(int count, Action<int> onSold)
    {
        Logger.Debug("[INFO] Sell grid: " + string.Join(", ", ExoticMerchant.SellItems().Select(i => $"{i.Name} x{i.Quantity}")));

        var sold = 0;
        foreach (var name in SellOrder)
            while (sold < count && ExoticMerchant.TrySellOne(name))
            {
                sold++;
                yield return new WaitForSeconds(InteractionDelay);
            }

        Logger.Debug($"[INFO] Merchant: sold {sold}/{count}.");
        onSold(sold);
    }
}
