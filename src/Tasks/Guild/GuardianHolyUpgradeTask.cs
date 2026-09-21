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
///     Chaos Rift's guardian "Holy damage" upgrade (Guild -&gt; Chaos Rift -&gt; "Upgrades" action button -&gt;
///     Magic Quarters' "Chaos Rift" tab) - spends Orbs of Light (per the wiki's Chaos Rift page: 25%
///     of Chaos Rift damage dealt, used to upgrade guardian holy damage, resets every month, so no
///     reason to ever hold back spending it) - requested by the user, 2026-09-20, after confirming
///     live that the "GuardianHolyUpgrade" badge (real, present on the shared notification rail per
///     BotManager's own dump, but never wired to anything before now) opens exactly this screen.
///     Live-confirmed across several rounds of diagnostics, 2026-09-20: clicking Chaos Rift's own
///     "Upgrades" action button doesn't open a dedicated Chaos-Rift screen - it opens Magic Quarters
///     (the same screen GuardianTrainingTask uses), landing directly on its "Chaos Rift" tab (one of
///     5 guardian tabs total, per the user: training/evolution/Chaos Rift/rarity/skin). Loops all 4
///     guardians (guardianList/guardian (0)..(3), properly indexed - confirmed via a diagnostic
///     dump), clicking each one then spending on Holy Damage while affordable, since the upgrade
///     screen is per-guardian (see the user's screenshot: one guardian's stats/upgrade shown at a
///     time, switched via the same roster icons used by Guardian Training).
/// </summary>
public class GuardianHolyUpgradeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 100;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.GuardianHolyUpgrade;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(24);

    // 4 guardians per the wiki/GuardianTrainingTask's own config comment (Vermilion/Grace/Ankaa/Azhar).
    private const int GuardianCount = 4;

    // Same "keep going until nothing's left to buy" ceiling used by BeerExchange/TreeOfLife.
    private const int MaxUpgradesPerGuardian = 30;

    public override IEnumerator Execute()
    {
        // Fast path - opportunistic only, safe no-op if not up.
        yield return Notifications.GuardianHolyUpgrade;

        // Guaranteed path regardless of the notification - same reasoning as every other task.
        yield return TownGuild.Open;
        yield return TownGuild.OpenChaosRift;

        if (ChaosRift.IsVisible) yield return ChaosRift.OpenUpgrades;

        // Live-confirmed, 2026-09-20: reaching Magic Quarters via Chaos Rift's own "Upgrades" button
        // lands directly on the Chaos Rift tab already active - clicked anyway for robustness in
        // case some other entry path (e.g. the notification badge alone) lands on a different tab.
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
