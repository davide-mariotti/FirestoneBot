using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     War Machines (https://firestone-idle-rpg.fandom.com/wiki/War_Machines) leveling - the user
///     asked for a level-up logic here too, after Hall of Heroes. Per the wiki: leveling a war
///     machine costs Expedition Tokens plus 3 components (Screw/Cog/Metal, obtained passively from
///     jewel chests, auto-distributed across owned machines by the game itself) for +100 xp: "level
///     bonus = 1.05^(level-1) - 1", a flat multiplicative boost to all of that machine's attributes.
///     Live-confirmed, 2026-09-18: the level-up button's own clickable state does NOT reliably predict
///     real affordability - clicking it while short on Expedition Tokens pops the game's generic
///     "CurrencyMissing" warning instead of silently failing (same issue as Tree of Life's personal
///     upgrades - see CurrencyMissingPopup), so this stops the whole task as soon as that appears
///     instead of hitting the same wall on every remaining machine (Expedition Tokens are a single
///     currency shared across all of them, per the wiki).
///     Runs across every owned war machine (WarMachines.Machines), not just the 5 in the active
///     battle formation - unlike Hall of Heroes' gear T1, the wiki gives no indication that leveling
///     a benched machine is wasted (components are already auto-balanced across ALL owned machines by
///     the game itself, favoring newly unlocked ones), so there's no similar reason to scope down.
///     Screen has 3 other tabs not covered here - "garage" (read-only stats, no action at all),
///     "blueprintUpgrades" and "rarityUpgrades" (separate wiki mechanics: per-attribute currency
///     spend and big rarity-tier jumps respectively) - out of scope for "level up", not requested.
/// </summary>
public class WarMachinesTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    // No wiki-confirmed level for War Machines specifically (the mechanics page has no "Unlocks at"
    // infobox) - inferred from the Engineer building's own confirmed level-50 gate, since the wiki
    // says war machines "are unlocked by the Engineer". Flag for live verification if this is wrong.
    protected override int MinimumCharacterLevel => 50;

    internal override float? MaxRuntimeSeconds => 1800f;

    // Live-confirmed real, 2026-09-20 (BotManager's notification rail diagnostic dump). Not a
    // threshold gate - components are auto-distributed by the game (per the wiki, see class doc
    // comment), so "badge lit" means real progress is waiting to be spent.
    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Warmachines;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // Safety bound only, matching the pattern used everywhere else in this codebase for a "keep
    // going until nothing's left to do" loop.
    private const int MaxIterationsPerMachine = 200;

    public override IEnumerator Execute()
    {
        // Fast path - opportunistic only, safe no-op if not up (see NotificationBadgeName above).
        yield return Notifications.Warmachines;

        yield return TownScreen.Open;

        // Live-confirmed, 2026-09-18: War Machines lives behind the Engineer building's
        // "GarageSelection" choice popup's sibling "garage" card, not inside the Engineer screen
        // itself - see Town.OpenWarMachines.
        yield return TownScreen.OpenWarMachines;

        foreach (var machine in WarMachines.Machines)
        {
            yield return WarMachines.SelectMachine(machine);
            yield return WarMachines.OpenWorkshopTab;

            var iterations = 0;
            var outOfCurrency = false;

            while (WarMachines.LevelUpBtn.IsClickable() && iterations < MaxIterationsPerMachine)
            {
                iterations++;
                yield return WarMachines.LevelUpBtn.Click();

                if (!CurrencyMissingPopup.IsShowing) continue;

                yield return CurrencyMissingPopup.Close;
                outOfCurrency = true;
                break;
            }

            if (outOfCurrency) break;
        }

        yield return WarMachines.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
