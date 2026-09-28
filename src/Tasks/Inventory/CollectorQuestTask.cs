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
///     Progresses the daily quest "Collector" (open 4 gear chests) by opening just enough of the
///     CHEAPEST gear chests to satisfy it, keeping a reserve of Common so the next day isn't blocked
///     either (see min_common_reserve). Chest opening was never automated before.
///     Rewritten 2026-09-28: this used to empty every gear rarity in the bag, every hour. The F2P
///     guide lists that among the mistakes to avoid - uncommon and above are what you hoard so a
///     newly unlocked hero can have its gear tiers 1/2/3 opened in one go. Now only the slots in
///     ExpendableGearSlots are ever opened, and only up to GearChestTarget per day.
///     Claiming the quest reward itself is QuestsTask's job (Task 16); this only needs to make the
///     underlying objective progress, which the wiki says updates immediately, no extra step needed.
///     Gear chests are now addressed by explicit slot name (ExpendableGearSlots) rather than by
///     scanning for "anything that isn't a known non-chest slot" - the whole point is to open some
///     rarities and not others, which a generic scan can't express. The names in that list are the
///     low rarities confirmed live; anything not in it is simply never opened, which is the safe
///     direction if a rarity's real slot name differs from what's assumed.
///     Still opens jewel/celestial chests in full (on the user's request, since those otherwise pile
///     up unopened - notably from Pharaoh's Vault rewards) even though they don't count toward the
///     "Collector" quest itself, which only requires gear chests per the wiki.
///     Per the user (2026-09-23): once 4 gear chests are opened today, stop the WHOLE task (including
///     jewel/celestial) until the date changes - previously this reopened every chest slot every 6h
///     regardless of how many gear chests had already been opened that day, wasting clicks across 16
///     bot instances. jewelChest/celestialChest never count toward the tracked total (see
///     GearChestTarget), matching the wiki's actual quest requirement.
/// </summary>
public class CollectorQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;

    // Per the user (2026-09-26): recheck hourly instead of every 6h, so newly-accumulated chests get
    // opened promptly instead of piling up in the inventory across a fleet of many instances.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(1);

    // The "Collector" quest's exact requirement - see class doc comment.
    private const int GearChestTarget = 4;

    // Not counted toward GearChestTarget - see class doc comment.
    private static readonly HashSet<string> NonGearChestSlots = new() { "jewelChest", "celestialChest" };

    /// <summary>
    ///     The ONLY gear chest slots this task ever opens, cheapest first - everything above them
    ///     (Uncommon, Rare, Epic, Legendary, Titan...) is deliberately left to pile up. Per the F2P
    ///     guide those are what you save for a newly unlocked hero, to unlock its gear tiers 1/2/3 in
    ///     one go, and opening every chest as it arrives is on the guide's own list of mistakes to
    ///     avoid. Until 2026-09-28 this task emptied every rarity every hour, which is exactly that
    ///     mistake.
    ///     "Wooden" and "Iron" are live-confirmed slot names (2026-09-26, see Paths.InventoryLoc) and
    ///     sit below Common, so they're spent before it. That ordering is the one assumption here; if
    ///     it's wrong the cost is only which cheap chest gets used for the quest, never a hoarded one.
    /// </summary>
    private static readonly string[] ExpendableGearSlots =
    {
        "/Wooden", "/Iron", Paths.InventoryLoc.CommonChestSlot
    };

    // Live-confirmed, 2026-09-17: the Chests tab's real gear-chest slots (5 populated slots seen on
    // screen: commonChestbox + 4 others) don't exist yet in Content.GetChildren() right after
    // OpenChestsTab's own 1s interaction_delay - only 2 always-present placeholder slots
    // (jewelChest/celestialChest) were there that soon. They're instantiated dynamically a moment
    // later. This extra wait is separate from BotSettings.InteractionDelay (which covers click
    // response time, not this list's own populate delay) - not a click, so IsClickable-gated Click()
    // doesn't apply here.
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

        // Jewel/celestial chests: still emptied completely. They don't count toward the Collector
        // quest (see NonGearChestSlots) and the F2P guide actively wants the Oracle's celestial
        // chests opened, so there's nothing to hoard here.
        foreach (var name in slotNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            if (!NonGearChestSlots.Contains(name)) continue;

            yield return ChestOpening.OpenAll("/" + name);
        }

        // Gear chests: only as many as today's quest still needs, and only from the cheapest
        // rarities - see ExpendableGearSlots.
        var minReserve = _minCommonReserve?.Value ?? 10;
        var stillNeeded = GearChestTarget - (_gearChestsOpenedToday?.Value ?? 0);

        foreach (var slot in ExpendableGearSlots)
        {
            if (stillNeeded <= 0) break;

            // The reserve only guards Common, the slot the config entry has always been about.
            var reserve = slot == Paths.InventoryLoc.CommonChestSlot ? minReserve : 0;

            yield return ChestOpening.OpenDownTo(slot, reserve, opened =>
            {
                gearOpenedThisRun += opened;
                stillNeeded -= opened;
            }, stillNeeded);
        }

        yield return InventoryScreen.Close;

        if (_gearChestsOpenedToday != null)
            _gearChestsOpenedToday.Value = (_gearChestsOpenedToday.Value) + gearOpenedThisRun;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
