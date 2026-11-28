using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MagicQuartersScreen = Firebot.GameModel.Features.Town.MagicQuarters.MagicQuarters;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Evolves every guardian that can be: Magic Quarters -> evolution tab. The game's data says which
///     guardians are ready to evolve, so only those are clicked, and with none the screen stays closed.
/// </summary>
public class GuardianEvolutionTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    // A guardian "Ready to Evolve" lights GuardianTraining too, for hours (Steam-18 and -19, 05/11).
    protected override string[] NotificationPaths => RailBadges(Paths.BattleLoc.NotificationsLoc.GuardianEvolution,
        Paths.BattleLoc.NotificationsLoc.GuardianTraining);

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(12);

    public override IEnumerator Execute()
    {
        // 10-15 s a run before, clicking all four guardians to find none ready (Steam-27, 27/11).
        var guardians = MagicQuartersScreen.Unlocked;
        if (guardians != null && !guardians.Any(g => g.readyToEvole))
        {
            Debug($"[INFO] From the game: no guardian ready to evolve " +
                  $"({string.Join(", ", guardians.Select(g => $"{g.guardianName} evolution {g.evolutionStage + 1} level {g.level}"))}) - not opening.");
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return Notifications.GuardianEvolution;

        yield return TownScreen.Open;
        yield return TownScreen.OpenMagicQuarters;

        // The game's own flag on each square; every unlocked one when it can't be read.
        var squares = MagicQuartersScreen.Squares().Where(s => s.guardian.IsUnlocked && (guardians == null || s.guardian.readyToEvole)).ToList();
        foreach (var square in squares)
        {
            var name = square.guardian.guardianName;
            yield return new GameButton(transform: square.transform).Click();
            yield return MagicQuartersScreen.OpenEvolutionTab;

            var evolveBtn = MagicQuartersScreen.EvolveBtn;
            if (!evolveBtn.IsClickable()) continue;

            // Strange Dust as seen live; anything else (gems) is never paid.
            var cost = MagicQuartersScreen.EvolveCost;
            var icon = MagicQuartersScreen.EvolveCostIcon;
            if (icon != "strangeDust64")
            {
                Debug($"[INFO] Guardian {name}: evolution costs '{cost}' in '{icon}', not Strange Dust - skipped.");
                continue;
            }

            yield return evolveBtn.Click();
            yield return Poll.Until(() => !evolveBtn.IsClickable() || MagicQuartersScreen.EvolveCost != cost);

            var evolved = !evolveBtn.IsClickable() || MagicQuartersScreen.EvolveCost != cost;
            Debug(evolved
                ? $"[INFO] Guardian {name} evolved for {cost} Strange Dust."
                : $"[FAILED] Guardian {name}: evolve clicked ({cost} Strange Dust) but the button didn't change.");
        }

        // A locked guardian's click opens LockedGuardian.
        yield return MagicQuartersScreen.CloseLockedPopup;
        yield return MagicQuartersScreen.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
