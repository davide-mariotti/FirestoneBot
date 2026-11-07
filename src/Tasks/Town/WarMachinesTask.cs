using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Logger = Firebot.Core.Logger;
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

    // Expedition Tokens are shared with TreeOfLifeTask and a level here costs 500, so running at 500
    // starves the tree's dearer nodes. The user's rule (07/11): the Personal Tree comes first, and
    // until it reaches this level the Warmachines badge stays lit on purpose. Past it this comes back
    // by itself, so "enabled" stays true.
    private const int TreeLevelFirst = 50;

    private static bool TreeIsDone => TreeOfLife.PersonalLevel >= TreeLevelFirst;

    // Assumed from the Engineer building's level-50 unlock; the wiki gives none for war machines.
    protected override int MinimumCharacterLevel => 50;

    internal override float? MaxRuntimeSeconds => 1800f;

    // The game hands components out by itself, so a lit badge means there's leveling to do.
    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Warmachines;

    public override bool IsNotificationVisible() => base.IsNotificationVisible() && TreeIsDone;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private const int MaxIterationsPerMachine = 200;

    public override IEnumerator Execute()
    {
        if (!TreeIsDone)
        {
            Logger.Debug($"[WarMachinesTask] [INFO] Waiting for the Personal Tree: level {TreeOfLife.PersonalLevel} of {TreeLevelFirst}.");
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return Notifications.Warmachines;

        yield return TownScreen.Open;

        yield return TownScreen.OpenWarMachines;

        var levels = 0;
        var outOfCurrency = false;
        foreach (var machine in WarMachines.Machines)
        {
            yield return WarMachines.SelectMachine(machine);
            yield return WarMachines.OpenWorkshopTab;

            var iterations = 0;

            while (WarMachines.LevelUpBtn.IsClickable() && iterations < MaxIterationsPerMachine)
            {
                iterations++;
                levels++;
                yield return WarMachines.LevelUpBtn.Click();

                if (!CurrencyMissingPopup.IsShowing) continue;

                yield return CurrencyMissingPopup.Close;
                outOfCurrency = true;
                break;
            }

            if (outOfCurrency) break;
        }

        Logger.Debug($"[WarMachinesTask] [INFO] {levels} click(s), out of tokens or components: {outOfCurrency}.");

        yield return WarMachines.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
