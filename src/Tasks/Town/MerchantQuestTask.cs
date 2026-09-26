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
using UnityEngine;
using InventoryScreen = Firebot.GameModel.Features.Inventory.Inventory;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Progresses the daily quest "Merchant" (sell 10 items at the Exotic Merchant) - level 30 per
///     the wiki quest table. Never automated before.
///     1. Uses up any "instant gold" items (Pouch/Bucket/Crate/Pile of Gold) from the bag FIRST -
///        these convert to meteorites and should never be sold (per the user).
///     2. Sells fixed quantities from the sell grid's first 4 slots (3+3+3+1 = 10) instead of
///        scanning for 10 different item types one each - simplified per the user, 2026-09-20: the
///        "one of each, up to 10 distinct types" approach silently under-delivered whenever fewer
///        than 10 sellable types were available that run, never actually completing the quest. Per
///        the user's own screenshot, the first 4 grid slots are always Scroll of Speed/Damage/Health
///        then Midas' Touch, always present in large enough quantities that selling a few of each
///        never risks depleting/hiding the slot mid-run (unlike the old approach, which had to dodge
///        a real grid-compaction bug from fully-depleted stacks disappearing) - gold items always
///        sort after these 4, per the user, so a defensive "skip if named gold" check is kept anyway
///        since skipping is always safe. Note this now DOES sell Midas' Touch (1x), reversing the
///        prior exclusion - explicit per the user this time, not the wiki-driven guess from before.
///     3. Spends the resulting exotic coins on one upgrade - whichever is cheapest/first affordable
///        in the currently-displayed tree (not scanning across all ~25 trees for the globally
///        cheapest - the user confirmed picking the first available is fine, and Exotic Upgrades
///        aren't the primary progression lever Firestone/Meteorite Research are).
///     4. User-requested: immediately claims the (now-completed) "Merchant" quest afterward instead
///        of waiting for QuestsTask's own schedule - reuses the same generic claim-every-completed-
///        quest routine (safe no-op on anything not actually claimable yet).
///     Per the user (2026-09-23): once today's quest is claimed, skip the whole routine (including
///     the gold-item usage) until the date changes - previously this re-sold items, re-bought an
///     upgrade and re-attempted the claim every 6h regardless of whether today's quest was already
///     done, wasting clicks across 16 bot instances.
/// </summary>
public class MerchantQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;
    protected override int MinimumCharacterLevel => 30;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // Exactly what the "Merchant" quest requires - see class doc comment.
    private const int SellTarget = 10;

    // Grid index -> how many times to sell from that slot, in on-screen left-to-right order
    // (Scroll of Speed, Scroll of Damage, Scroll of Health, Midas' Touch) - sums to SellTarget.
    private static readonly int[] SellCountsByGridIndex = { 3, 3, 3, 1 };

    // Defensive only - see class doc comment. Gold items are used separately (see
    // InventoryScreen.UseAllGoldItems), never sold.
    private const string NeverSellTerm = "gold";

    // Live-confirmed, 2026-09-18: same pooled-ScrollView populate delay as Collector Quest's chest
    // list (see CollectorQuestTask.ChestListPopulateDelay) - switching to the Items tab doesn't
    // populate its real content instantly, so scanning for gold items right away found nothing.
    private static readonly WaitForSeconds ItemListPopulateDelay = new(1.5f);

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

        yield return InventoryScreen.Open;
        yield return InventoryScreen.OpenItemsTab;
        yield return ItemListPopulateDelay;
        yield return InventoryScreen.UseAllGoldItems();
        yield return InventoryScreen.Close;

        yield return TownScreen.Open;
        yield return TownScreen.OpenExoticMerchant;
        yield return ExoticMerchant.OpenSellItemsTab;

        var sold = 0;
        for (var i = 0; i < SellCountsByGridIndex.Length; i++)
        {
            var item = ExoticMerchant.SellProductGrid.GetChild(i);
            if (item == null || item.Name.ToLowerInvariant().Contains(NeverSellTerm)) continue;

            var sellBtn = new GameButton(Paths.ExoticMerchantLoc.SellLoc.SellBtn, item);
            for (var n = 0; n < SellCountsByGridIndex[i]; n++)
            {
                if (!sellBtn.IsClickable()) break;
                yield return sellBtn.Click();
                sold++;
            }
        }

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
                break; // one upgrade per run is enough - see class doc
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
