using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Levels every owned war machine in the Workshop while Expedition Tokens last. The level-up
///     button stays clickable without tokens, so the CurrencyMissing popup is the stop signal - and
///     since the tokens are shared by all machines, it ends the whole run.
/// </summary>
public class WarMachinesTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    // Off by default though it works: Expedition Tokens are shared with TreeOfLifeTask and nothing
    // splits them, so running both lets whichever runs first drain them. The F2P guide says to
    // spend them in one place and picks the personal tree. Swap the two tasks' "enabled" to go the
    // War Machine route instead - never enable both.
    protected override bool DefaultEnabled => false;

    // Assumed from the Engineer building's level-50 unlock; the wiki gives none for war machines.
    protected override int MinimumCharacterLevel => 50;

    internal override float? MaxRuntimeSeconds => 1800f;

    // The game hands components out by itself, so a lit badge means there's leveling to do.
    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Warmachines;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private const int MaxIterationsPerMachine = 200;

    public override IEnumerator Execute()
    {
        yield return Notifications.Warmachines;

        yield return TownScreen.Open;

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
