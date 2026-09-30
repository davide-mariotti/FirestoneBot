using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MagicQuartersScreen = Firebot.GameModel.Features.Town.MagicQuarters.MagicQuarters;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Evolves every guardian that can be: Magic Quarters -> evolution tab. The button turns clickable
///     once the guardian reaches the level the next evolution needs and the Strange Dust is there.
/// </summary>
public class GuardianEvolutionTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.GuardianEvolution;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(12);

    // Vermilion, Grace, Ankaa, Azhar.
    private const int GuardianCount = 4;

    public override IEnumerator Execute()
    {
        yield return Notifications.GuardianEvolution;

        yield return TownScreen.Open;
        yield return TownScreen.OpenMagicQuarters;

        for (var i = 0; i < GuardianCount; i++)
        {
            var guardianBtn = new GameButton($"{Paths.MenusLoc.MagicQuartersLoc.GuardiansRoot}/guardian ({i})");
            if (!guardianBtn.IsClickable()) continue;

            yield return guardianBtn.Click();
            yield return MagicQuartersScreen.OpenEvolutionTab;

            var evolveBtn = MagicQuartersScreen.EvolveBtn;
            if (!evolveBtn.IsClickable()) continue;

            // Strange Dust as seen live; anything else (gems) is never paid.
            var cost = MagicQuartersScreen.EvolveCost;
            var icon = MagicQuartersScreen.EvolveCostIcon;
            if (icon != "strangeDust64")
            {
                Debug($"[INFO] Guardian {i}: evolution costs '{cost}' in '{icon}', not Strange Dust - skipped.");
                continue;
            }

            yield return evolveBtn.Click();
            yield return Poll.Until(() => !evolveBtn.IsClickable() || MagicQuartersScreen.EvolveCost != cost);

            var evolved = !evolveBtn.IsClickable() || MagicQuartersScreen.EvolveCost != cost;
            Debug(evolved
                ? $"[INFO] Guardian {i} evolved for {cost} Strange Dust."
                : $"[FAILED] Guardian {i}: evolve clicked ({cost} Strange Dust) but the button didn't change.");
        }

        // A locked guardian's click opens LockedGuardian.
        yield return MagicQuartersScreen.CloseLockedPopup;
        yield return MagicQuartersScreen.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
