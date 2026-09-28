using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     The daily "Merchant" quest: sells 10 items at the Exotic Merchant, spends the coins on the
///     first affordable Exotic Upgrade, then claims the quest straight away. Only junk is ever sold
///     (see SellOrderByGridIndex); if the junk runs out before 10, the quest stays open for the day,
///     since one daily reward is worth far less than the scrolls. The instant gold items are never
///     sold nor used here. Once claimed, the task waits for the next game-day.
/// </summary>
public class MerchantQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;
    protected override int MinimumCharacterLevel => 30;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private const int SellTarget = 10;

    /// <summary>
    ///     The grid's first four slots are 0 Scroll of Speed, 1 Scroll of Damage, 2 Scroll of Health
    ///     and 3 Midas' Touch; the instant gold and meteorite consumables sort after them. Sold:
    ///     Midas' Touch first, then Health and Damage only to top up. Never sold: Speed - the F2P
    ///     guide's main gold multiplier - or anything past slot 3. An allowlist on purpose: a missing
    ///     entry costs one daily quest, while a blocklist missing a name could sell a Barrel.
    /// </summary>
    private static readonly int[] SellOrderByGridIndex = { 3, 2, 1 };

    // A second line of defence behind the allowlist, matched against the slots' GameObject names.
    private static readonly string[] NeverSellTerms = { "gold", "meteor" };

    private static bool IsNeverSell(string name) =>
        NeverSellTerms.Any(t => name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);

    private MelonPreferences_Entry<string> _lastDoneDate;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_lastDoneDate != null) return;

        _lastDoneDate = category.CreateEntry(
            "last_done_date",
            "",
            "Last Done Date",
            "(auto-managed, don't edit) - the last date today's Merchant quest was already claimed. " +
            "Skips the whole task until the date changes."
        );
    }

    public override IEnumerator Execute()
    {
        var today = GameDay.Today();
        if (_lastDoneDate?.Value == today)
        {
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return TownScreen.Open;
        yield return TownScreen.OpenExoticMerchant;
        yield return ExoticMerchant.OpenSellItemsTab;

        // The real slot names, for when the on-screen order ever needs re-checking.
        Debug("[INFO] Sell grid: " + string.Join(", ", ExoticMerchant.SellProductGrid.GetChildren()
            .Select((c, i) => $"{i}={c.Name}")));

        var sold = 0;
        foreach (var gridIndex in SellOrderByGridIndex)
        {
            if (sold >= SellTarget) break;

            var expectedName = ExoticMerchant.SellProductGrid.GetChild(gridIndex)?.Name;
            if (string.IsNullOrEmpty(expectedName) || IsNeverSell(expectedName)) continue;

            while (sold < SellTarget)
            {
                // A stack that sells out disappears and the grid compacts, sliding another item -
                // possibly a Barrel - into this index. Re-checking the name before every click is what
                // makes selling one slot down safe.
                var item = ExoticMerchant.SellProductGrid.GetChild(gridIndex);
                if (item == null || item.Name != expectedName) break;

                var sellBtn = new GameButton(Paths.ExoticMerchantLoc.SellLoc.SellBtn, item);
                if (!sellBtn.IsClickable()) break;

                yield return sellBtn.Click();
                sold++;
            }
        }

        if (sold < SellTarget)
            Debug($"[INFO] Only {sold}/{SellTarget} items sold - the sellable slots ran out, the quest stays open today.");

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

        if (sold >= SellTarget)
        {
            yield return CharacterScreen.Open();
            yield return CharacterScreen.OpenQuestsTab;
            yield return CharacterScreen.OpenDailyQuestsSubTab;
            foreach (var claimButton in CharacterScreen.DailyQuestClaimButtons())
                yield return claimButton.Click();
            yield return CharacterScreen.Close;

            if (_lastDoneDate != null) _lastDoneDate.Value = today;
        }

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
