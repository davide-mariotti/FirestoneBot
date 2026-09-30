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
///     rarity first, up to Legendary if nothing cheaper is left. The same run then opens ExtraChests
///     more, cheap rarities only and never Common below min_common_chest_reserve, so MerchantQuestTask
///     has items to sell. Jewel and celestial chests don't count for the quest and are always opened.
/// </summary>
public class CollectorQuestTask : DailyQuestTask
{
    protected override string QuestName => "Collector";

    // With the quest's 4, 10 chests a day.
    private const int ExtraChests = 6;

    private static readonly HashSet<string> NonGearChestSlots = new() { "jewelChest", "celestialChest" };

    // Cheapest first.
    private static readonly string[] QuestChestSlots =
    {
        Paths.InventoryLoc.WoodenChestSlot, Paths.InventoryLoc.IronChestSlot, Paths.InventoryLoc.CommonChestSlot,
        Paths.InventoryLoc.UncommonChestSlot, Paths.InventoryLoc.RareChestSlot, Paths.InventoryLoc.EpicChestSlot,
        Paths.InventoryLoc.LegendaryChestSlot
    };

    private static readonly string[] ExtraChestSlots =
    {
        Paths.InventoryLoc.WoodenChestSlot, Paths.InventoryLoc.IronChestSlot, Paths.InventoryLoc.CommonChestSlot
    };

    // The gear chest slots are created a moment after the tab opens, later than the interaction delay.
    private static readonly WaitForSeconds ChestListPopulateDelay = new(1.5f);

    private MelonPreferences_Entry<int> _minCommonReserve;

    protected override void OnConfigureQuest(MelonPreferences_Category category)
    {
        _minCommonReserve = category.CreateEntry(
            "min_common_chest_reserve",
            10,
            "Minimum Common Chest Reserve",
            "The extra chests opened for the Merchant quest never take Common chests below this count. The " +
            "Collector quest itself opens whatever it needs. Default: 10."
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
        foreach (var slot in ExtraChestSlots)
        {
            if (extraOpened >= ExtraChests) break;
            var reserve = slot == Paths.InventoryLoc.CommonChestSlot ? _minCommonReserve?.Value ?? 10 : 0;
            yield return ChestOpening.OpenDownTo(slot, reserve, n => extraOpened += n, ExtraChests - extraOpened);
        }

        Debug($"[INFO] Collector: {questOpened}/{missing} chest(s) for the quest, {extraOpened}/{ExtraChests} extra.");

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
