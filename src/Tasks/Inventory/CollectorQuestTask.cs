using System;
using System.Collections;
using System.Collections.Generic;
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
///     rarity first, up to Legendary if nothing cheaper is left. The same run then opens every gear
///     chest above gear_chest_reserve per rarity: the contents don't grow with level and the items feed
///     MerchantQuestTask, so a stock past that only waits. Jewel and celestial chests don't count for
///     the quest and are always opened.
/// </summary>
public class CollectorQuestTask : DailyQuestTask
{
    protected override string QuestName => "Collector";

    private static readonly HashSet<string> NonGearChestSlots = new() { "jewelChest", "celestialChest" };

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
            "Gear chests kept per rarity (Wooden to Legendary) for the next Collector quests, event " +
            "challenges and a new hero's gear; every one above it is opened. The Collector quest itself " +
            "opens whatever it needs. Default: 10."
        );
    }

    protected override IEnumerator Work(int missing)
    {
        yield return OpenChestsTab();

        foreach (var name in InventoryScreen.Content.GetChildren().Select(c => c.Name).ToList())
            if (NonGearChestSlots.Contains(name))
                yield return ChestOpening.OpenAll("/" + name);

        var questOpened = 0;
        yield return OpenCheapestFirst(missing, n => questOpened = n);

        var extraOpened = 0;
        foreach (var slot in QuestChestSlots)
            yield return ChestOpening.OpenDownTo(slot, _reserve?.Value ?? 10, n => extraOpened += n);

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
