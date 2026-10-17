using System;
using System.Collections;
using System.Linq;
using Firebot.Infrastructure;
using MelonLoader;
using UnityEngine;
using Logger = Firebot.Core.Logger;
using ChestOpening = Firebot.GameModel.Features.Inventory.ChestOpening;
using InventoryScreen = Firebot.GameModel.Features.Inventory.Inventory;

namespace Firebot.Tasks.Inventory;

/// <summary>
///     The daily "Collector" quest (open 4 chests) comes first: the missing chests are opened cheapest
///     rarity first, up to Legendary if nothing cheaper is left. The same run then opens every other
///     chest in the bag: those rarities down to gear_chest_reserve, any other slot (Golden, Diamond, the
///     celestials) all of it. Chest contents don't grow with level, so a stock past that only waits.
/// </summary>
public class CollectorQuestTask : DailyQuestTask
{
    protected override string QuestName => "Collector";

    // The chests above the reserve are opened even when an event challenge has already done the quest.
    protected override bool WorksWhenComplete => true;

    // Cheapest first.
    private static readonly string[] QuestChestSlots =
    {
        Paths.InventoryLoc.WoodenChestSlot, Paths.InventoryLoc.IronChestSlot, Paths.InventoryLoc.CommonChestSlot,
        Paths.InventoryLoc.UncommonChestSlot, Paths.InventoryLoc.RareChestSlot, Paths.InventoryLoc.EpicChestSlot,
        Paths.InventoryLoc.LegendaryChestSlot
    };

    // The gear chest slots are created a moment after the tab opens, later than the interaction delay.
    private static readonly WaitForSeconds ChestListPopulateDelay = new(1.5f);

    private MelonPreferences_Entry<int> _reserve;

    protected override void OnConfigureQuest(MelonPreferences_Category category)
    {
        _reserve = category.CreateEntry(
            "gear_chest_reserve",
            10,
            "Gear Chest Reserve",
            "Chests kept per rarity the Collector quest uses (Wooden to Legendary) for the next quests, " +
            "event challenges and a new hero's gear; every one above it, and every other chest, is opened. " +
            "The Collector quest itself opens whatever it needs. Default: 10."
        );
    }

    protected override IEnumerator Work(int missing)
    {
        yield return OpenChestsTab();

        var questOpened = 0;
        yield return OpenCheapestFirst(missing, n => questOpened = n);

        // A slot is named after its chest; the jewelChest and celestialChest children are hidden
        // templates (Steam-0, 17/10), so Golden and the celestials only show up in this list.
        var reserve = _reserve?.Value ?? 10;
        var extraOpened = 0;
        foreach (var slot in InventoryScreen.Content.GetChildren().Select(c => "/" + c.Name).ToList())
            yield return ChestOpening.OpenDownTo(slot, QuestChestSlots.Contains(slot) ? reserve : 0,
                n => extraOpened += n);

        Debug($"[INFO] Collector: {questOpened}/{missing} chest(s) for the quest, {extraOpened} above the reserve.");

        yield return InventoryScreen.Close;
    }

    /// <summary>An event challenge's chests (EventChallengeActions): up to `count`, cheapest first.</summary>
    public static IEnumerator OpenChests(int count, Action<int> onOpened)
    {
        yield return OpenChestsTab();

        var opened = 0;
        yield return OpenCheapestFirst(count, n => opened = n);
        Logger.Debug($"[INFO] Chests: opened {opened}/{count}.");
        onOpened(opened);

        yield return InventoryScreen.Close;
    }

    private static IEnumerator OpenChestsTab()
    {
        yield return InventoryScreen.Open;
        yield return InventoryScreen.OpenChestsTab;
        yield return ChestListPopulateDelay;
    }

    // Up to Legendary if nothing cheaper is left: what asks for them comes first.
    private static IEnumerator OpenCheapestFirst(int count, Action<int> onOpened)
    {
        var opened = 0;
        foreach (var slot in QuestChestSlots)
        {
            if (opened >= count) break;
            yield return ChestOpening.OpenDownTo(slot, 0, n => opened += n, count - opened);
        }

        onOpened(opened);
    }
}
