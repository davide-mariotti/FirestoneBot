using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Raises war machine rarity with Tools (the Engineer's), machine by machine in grid order. The
///     rarity button is only clickable once the Tools cover it.
/// </summary>
public class WarMachineRarityTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    protected override int MinimumCharacterLevel => 50;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.WarMachinesRarity;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(12);

    private const int MaxUpgradesPerMachine = 5;

    public override IEnumerator Execute()
    {
        // The game's own check, Tools and level requirement both (WarMachineHandler); 18 s a run to see it.
        if (GameState.Get("war machine rarity", () => GameState.H.WarMachineHandler.CanAnyWarmachineRarityUpgrade()) == false)
        {
            Debug($"[INFO] From the game: no war machine can rise in rarity " +
                  $"({GameState.Currency(Il2Cpp.CurrencyCode.Tools):0} Tools) - not opening.");
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return Notifications.WarMachinesRarity;

        yield return TownScreen.Open;
        yield return TownScreen.OpenWarMachines;

        var logged = false;
        foreach (var machine in WarMachines.Machines)
        {
            yield return WarMachines.SelectMachine(machine);
            yield return WarMachines.OpenRarityTab;

            // The Tools counter only exists while the rarity tab is open.
            if (!logged) Debug($"[INFO] War machine rarity: {WarMachines.Tools} Tools.");
            logged = true;

            for (var n = 0; n < MaxUpgradesPerMachine && WarMachines.RarityBtn.IsClickable(); n++)
            {
                var cost = WarMachines.RarityCost;
                var icon = WarMachines.RarityCostIcon;
                if (icon != "toolsIcon64")
                {
                    Debug($"[INFO] {machine.Name}: rarity costs '{cost}' in '{icon}', not Tools - skipped.");
                    break;
                }

                var before = WarMachines.Tools;
                yield return WarMachines.RarityBtn.Click();
                yield return Poll.Until(() => WarMachines.Tools < before);

                var after = WarMachines.Tools;
                if (after >= before)
                {
                    Debug($"[FAILED] {machine.Name}: rarity clicked ({cost} Tools) but Tools stayed at {before}.");
                    break;
                }

                Debug($"[INFO] {machine.Name}: rarity raised for {cost} Tools, {before} -> {after}.");
            }
        }

        yield return WarMachines.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
