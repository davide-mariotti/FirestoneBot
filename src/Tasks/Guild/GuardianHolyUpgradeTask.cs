using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MagicQuartersScreen = Firebot.GameModel.Features.Town.MagicQuarters.MagicQuarters;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Spends Orbs of Light on every guardian's holy damage: Chaos Rift -> Upgrades, which opens Magic
///     Quarters on its Chaos Rift tab. The orbs reset every month, so there's nothing to save them for.
/// </summary>
public class GuardianHolyUpgradeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 100;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.GuardianHolyUpgrade;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(24);

    // Vermilion, Grace, Ankaa, Azhar.
    private const int GuardianCount = 4;

    private const int MaxUpgradesPerGuardian = 30;

    public override IEnumerator Execute()
    {
        yield return Notifications.GuardianHolyUpgrade;

        yield return TownGuild.Open;
        yield return TownGuild.OpenChaosRift;

        if (ChaosRift.IsVisible) yield return ChaosRift.OpenUpgrades;

        // Already the active tab when coming from Chaos Rift; clicked for any other way in.
        yield return MagicQuartersScreen.OpenChaosRiftTab;

        for (var i = 0; i < GuardianCount; i++)
        {
            var guardianBtn = new GameButton($"{Paths.MenusLoc.MagicQuartersLoc.GuardiansRoot}/guardian ({i})");
            if (!guardianBtn.IsClickable()) continue;

            yield return guardianBtn.Click();

            var upgradeBtn = MagicQuartersScreen.ChaosRiftUpgradeBtn;
            for (var n = 0; n < MaxUpgradesPerGuardian; n++)
            {
                if (!upgradeBtn.IsClickable()) break;
                yield return upgradeBtn.Click();
            }
        }

        yield return MagicQuartersScreen.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
