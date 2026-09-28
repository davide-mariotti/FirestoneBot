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
///     Progresses the daily quest "Merchant" (sell 10 items at the Exotic Merchant) - level 30 per
///     the wiki quest table. Never automated before.
///     1. Sells up to 10 items from an ALLOWLIST of grid positions, junk first - see
///        SellOrderByGridIndex for the full reasoning. Short version: Midas' Touch (slot 3) is sold
///        without limit up to the quest target, Scroll of Health and Scroll of Damage (slots 2 and 1)
///        only top it up, and Scroll of Speed (slot 0) is never sold at all because the F2P guide
///        makes it the centrepiece of the push routine. Nothing at index 4 or beyond is touched:
///        that's where the instant gold and meteorite consumables live (Pouch/Bucket/Crate/Barrel/
///        Pile), which per the user must never be sold - and which this task never opens or uses
///        either (2026-09-26: too valuable to consume generically, they need a deliberate approach,
///        not an automatic sweep).
///        Rewritten 2026-09-28 from fixed per-slot counts ({3,3,3,1} over slots 0-3), which sold 3
///        Scroll of Speed, 3 Damage and 3 Health every single day. Because one slot can now be sold
///        down an arbitrary number of times, the grid-compaction hazard the fixed counts used to dodge
///        is real again (a depleted stack disappears and slides a different item into that index), so
///        the slot is re-resolved and its name re-checked before every single click.
///        If the allowlisted slots can't produce 10, today's quest simply stays open and that's
///        logged - a deliberate trade: one daily reward is worth far less than the sold scrolls.
///     2. Spends the resulting exotic coins on one upgrade - whichever is cheapest/first affordable
///        in the currently-displayed tree (not scanning across all ~25 trees for the globally
///        cheapest - the user confirmed picking the first available is fine, and Exotic Upgrades
///        aren't the primary progression lever Firestone/Meteorite Research are).
///     3. User-requested: immediately claims the (now-completed) "Merchant" quest afterward instead
///        of waiting for QuestsTask's own schedule - reuses the same generic claim-every-completed-
///        quest routine (safe no-op on anything not actually claimable yet).
///     Per the user (2026-09-23): once today's quest is claimed, skip the whole routine until the
///     date changes - previously this re-sold items, re-bought an upgrade and re-attempted the claim
///     every 6h regardless of whether today's quest was already done, wasting clicks across 16 bot
///     instances.
/// </summary>
public class MerchantQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;
    protected override int MinimumCharacterLevel => 30;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // Exactly what the "Merchant" quest requires - see class doc comment.
    private const int SellTarget = 10;

    /// <summary>
    ///     Which grid slots may be sold, in the order they should be given up - an ALLOWLIST, and
    ///     nothing outside it is ever clicked. Rewritten 2026-09-28: the previous version sold fixed
    ///     counts by index ({3,3,3,1} over slots 0-3), i.e. 3 Scroll of Speed, 3 Damage and 3 Health
    ///     every single day. The F2P guide calls the Scroll of Speed the main gold multiplier of the
    ///     whole push routine (~x3 gold) and wants Damage/Health kept for the "all three active"
    ///     triple-damage talent; only the junk (Midas' Touch and friends) is supposed to be sold.
    ///     On-screen left-to-right order, confirmed by the user: 0 Scroll of Speed, 1 Scroll of
    ///     Damage, 2 Scroll of Health, 3 Midas' Touch - with the instant gold items (Pouch/Bucket/
    ///     Crate/Barrel/Pile) and everything else sorting after those four.
    ///     So: slot 3 first (pure junk per the guide, sold without limit up to the quest target),
    ///     then 2 and 1 only as top-up. Slot 0 is never sold, and neither is anything at index 4 or
    ///     beyond - that's where the instant gold and meteorite consumables live, which the user was
    ///     explicit about never selling. An allowlist is the right direction to be wrong in here: a
    ///     missing entry costs one daily quest, whereas a blocklist that misses a name sells a Barrel,
    ///     which doesn't come back.
    /// </summary>
    private static readonly int[] SellOrderByGridIndex = { 3, 2, 1 };

    // Defensive second line only - the index allowlist above is what actually protects the valuable
    // items. Names are GameObject names (see the grid dump logged at the start of every run): any
    // slot whose name looks like an instant gold or meteorite consumable is skipped even if it
    // somehow turned up at an allowlisted index.
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

        // One-off dump of the real grid contents. The allowlist above works purely by position, so
        // this isn't load-bearing - it's here so the actual GameObject names can be read off a live
        // run and the filter tightened by name later if the on-screen order ever changes.
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
                // Re-resolved every iteration, never cached: a stack that empties out disappears and
                // the grid compacts, sliding a DIFFERENT item into this index - possibly a Barrel.
                // The 2026-09-18 fix learned this the hard way on the old gold-item path. Bailing out
                // the moment the name at this index stops matching is what makes selling an unbounded
                // number from one slot safe.
                var item = ExoticMerchant.SellProductGrid.GetChild(gridIndex);
                if (item == null || item.Name != expectedName) break;

                var sellBtn = new GameButton(Paths.ExoticMerchantLoc.SellLoc.SellBtn, item);
                if (!sellBtn.IsClickable()) break;

                yield return sellBtn.Click();
                sold++;
            }
        }

        if (sold < SellTarget)
            Debug($"[INFO] Only {sold}/{SellTarget} items sold - today's Merchant quest stays open. " +
                  "Deliberate: the sellable slots ran out and the valuable ones are never touched.");

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
