using System.Collections;
using System.Linq;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;
using static Firebot.Core.BotSettings;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     The daily "Merchant" quest: sells as many items at the Exotic Merchant as the quest is missing,
///     then spends the coins on the first affordable Exotic Upgrade. Only junk is ever sold (see
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

        Debug("[INFO] Sell grid: " + string.Join(", ", ExoticMerchant.SellItems().Select(i => $"{i.Name} x{i.Quantity}")));

        var sold = 0;
        foreach (var name in SellOrder)
            while (sold < missing && ExoticMerchant.TrySellOne(name))
            {
                sold++;
                yield return new WaitForSeconds(InteractionDelay);
            }

        Debug($"[INFO] Merchant: sold {sold}/{missing}.");

        yield return ExoticMerchant.OpenUpgradesTab;

        var upgradeSlotNames = ExoticMerchant.UpgradesList.GetChildren().Select(c => c.Name).ToList();
        foreach (var name in upgradeSlotNames)
        {
            if (string.IsNullOrEmpty(name)) continue;

            var upgradeBtn = new GameButton(
                "/" + name + Paths.ExoticMerchantLoc.UpgradesLoc.UpgradeBtn, ExoticMerchant.UpgradesList);
            if (upgradeBtn.IsClickable())
            {
                yield return upgradeBtn.Click();
                break; // one upgrade per run
            }
        }

        yield return ExoticMerchant.Close;
        yield return TownScreen.Close;
    }
}
