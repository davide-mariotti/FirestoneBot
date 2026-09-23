using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.Infrastructure;
using MelonLoader;
using UnityEngine;
using ChestOpening = Firebot.GameModel.Features.Inventory.ChestOpening;
using InventoryScreen = Firebot.GameModel.Features.Inventory.Inventory;

namespace Firebot.Tasks.Inventory;

/// <summary>
///     Progresses the daily quest "Collector" (open 4 gear chests) by opening every chest in the bag
///     except a reserve of common gear chests (kept so the quest is never blocked the next day too -
///     see min_common_reserve). Chest opening was never automated before.
///     Claiming the quest reward itself is QuestsTask's job (Task 16); this only needs to make the
///     underlying objective progress, which the wiki says updates immediately, no extra step needed.
///     Only "commonChestbox" has a confirmed exact name (via UnityPy) among the 7 gear chest
///     rarities - the other 6 (uncommon..titan) have no uniquely-named template found statically, so
///     this scans every OTHER slot in the chests tab and opens it fully, skipping only the known
///     non-chest slots (mystery box claim, stat consumables, etc - see Paths.InventoryLoc). Flag for
///     live verification: if a rarity slot has a different real name than assumed, it's still
///     reachable through this generic scan since nothing here depends on knowing the name in advance.
///     Also opens jewel/celestial chests this way (on the user's request, since those otherwise pile
///     up unopened - notably from Pharaoh's Vault rewards) even though they don't count toward the
///     "Collector" quest itself, which only requires gear chests per the wiki - simplest to fold into
///     this same generic scan rather than a separate task.
///     Per the user (2026-09-23): once 4 gear chests are opened today, stop the WHOLE task (including
///     jewel/celestial) until the date changes - previously this reopened every chest slot every 6h
///     regardless of how many gear chests had already been opened that day, wasting clicks across 16
///     bot instances. jewelChest/celestialChest never count toward the tracked total (see
///     GearChestTarget), matching the wiki's actual quest requirement.
/// </summary>
public class CollectorQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // The "Collector" quest's exact requirement - see class doc comment.
    private const int GearChestTarget = 4;

    // Not counted toward GearChestTarget - see class doc comment.
    private static readonly HashSet<string> NonGearChestSlots = new() { "jewelChest", "celestialChest" };

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
        var today = DateTime.Now.ToString("yyyy-MM-dd");

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
        var nonChestSlots = new HashSet<string>(Paths.InventoryLoc.KnownNonChestSlots);
        var gearOpenedThisRun = 0;

        foreach (var name in slotNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            if (name == "Common") continue; // handled last, with a reserve
            if (nonChestSlots.Contains(name)) continue;
            if (name.StartsWith("emptySlot")) continue;

            var isGear = !NonGearChestSlots.Contains(name);
            yield return ChestOpening.OpenAll("/" + name, opened =>
            {
                if (isGear) gearOpenedThisRun += opened;
            });
        }

        var minReserve = _minCommonReserve?.Value ?? 10;
        yield return ChestOpening.OpenDownTo(
            Paths.InventoryLoc.CommonChestSlot, minReserve, opened => gearOpenedThisRun += opened);

        yield return InventoryScreen.Close;

        if (_gearChestsOpenedToday != null)
            _gearChestsOpenedToday.Value = (_gearChestsOpenedToday.Value) + gearOpenedThisRun;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
