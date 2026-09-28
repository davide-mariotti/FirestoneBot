using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;
using UnityEngine;
using ChestOpening = Firebot.GameModel.Features.Inventory.ChestOpening;
using InventoryScreen = Firebot.GameModel.Features.Inventory.Inventory;

namespace Firebot.Tasks.Inventory;

/// <summary>
///     The daily "Collector" quest (open 4 gear chests), using only the cheapest rarities and never
///     more than the quest needs. Uncommon and up are left to pile up: the F2P guide saves them for a
///     newly unlocked hero, and opening chests as they arrive is on its list of mistakes. Jewel and
///     celestial chests don't count for the quest and are always opened in full. QuestsTask claims
///     the reward. Once 4 gear chests are done, the whole task waits for the next game-day.
/// </summary>
public class CollectorQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(1);

    private const int GearChestTarget = 4;

    private static readonly HashSet<string> NonGearChestSlots = new() { "jewelChest", "celestialChest" };

    // Cheapest first. Wooden and Iron are assumed to rank below Common; if that's wrong, the only
    // effect is which cheap chest the quest uses.
    private static readonly string[] ExpendableGearSlots =
    {
        Paths.InventoryLoc.WoodenChestSlot, Paths.InventoryLoc.IronChestSlot, Paths.InventoryLoc.CommonChestSlot
    };

    // The gear chest slots are created a moment after the tab opens, later than the interaction delay.
    private static readonly WaitForSeconds ChestListPopulateDelay = new(1.5f);

    private MelonPreferences_Entry<int> _minCommonReserve;
    private MelonPreferences_Entry<int> _gearChestsOpenedToday;
    private MelonPreferences_Entry<string> _gearChestsDate;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_minCommonReserve != null) return;

        _minCommonReserve = category.CreateEntry(
            "min_common_chest_reserve",
            10,
            "Minimum Common Chest Reserve",
            "Common gear chests are never opened below this count, so there's always at least one " +
            "left to open for tomorrow's Collector quest too. Default: 10."
        );

        _gearChestsOpenedToday = category.CreateEntry(
            "gear_chests_opened_today",
            0,
            "Gear Chests Opened Today",
            "(auto-managed, don't edit) - how many gear chests are already opened today. Resets " +
            "automatically once the date changes."
        );

        _gearChestsDate = category.CreateEntry(
            "gear_chests_date",
            "",
            "Gear Chests Date",
            "(auto-managed, don't edit) - the date gear_chests_opened_today is counting for."
        );
    }

    public override IEnumerator Execute()
    {
        var today = GameDay.Today();

        if (_gearChestsDate?.Value != today)
        {
            if (_gearChestsOpenedToday != null) _gearChestsOpenedToday.Value = 0;
            if (_gearChestsDate != null) _gearChestsDate.Value = today;
        }

        if (_gearChestsOpenedToday?.Value >= GearChestTarget)
        {
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return InventoryScreen.Open;
        yield return InventoryScreen.OpenChestsTab;
        yield return ChestListPopulateDelay;

        var slotNames = InventoryScreen.Content.GetChildren().Select(c => c.Name).ToList();
        var gearOpenedThisRun = 0;

        foreach (var name in slotNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            if (!NonGearChestSlots.Contains(name)) continue;

            yield return ChestOpening.OpenAll("/" + name);
        }

        var minReserve = _minCommonReserve?.Value ?? 10;
        var stillNeeded = GearChestTarget - (_gearChestsOpenedToday?.Value ?? 0);

        foreach (var slot in ExpendableGearSlots)
        {
            if (stillNeeded <= 0) break;

            // The reserve setting is about Common chests only.
            var reserve = slot == Paths.InventoryLoc.CommonChestSlot ? minReserve : 0;

            yield return ChestOpening.OpenDownTo(slot, reserve, opened =>
            {
                gearOpenedThisRun += opened;
                stillNeeded -= opened;
            }, stillNeeded);
        }

        yield return InventoryScreen.Close;

        if (_gearChestsOpenedToday != null) _gearChestsOpenedToday.Value += gearOpenedThisRun;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
