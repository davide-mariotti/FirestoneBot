using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using MagicQuartersScreen = Firebot.GameModel.Features.Town.MagicQuarters.MagicQuarters;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Raises every guardian's rarity the game allows: Magic Quarters -> the guardian -> rarity tab. Its
///     evolution sets the ceiling (Uncommon at 5, Rare at 10, ...) and Contracts pay (30, 70, 150, ...): for
///     x61 guardian damage and x51 aura at Uncommon (wiki). Only the guardians the game's own test passes are
///     clicked, and with none the screen stays closed. Contracts are Hall of Heroes' hero rarity's too:
///     whichever runs first spends them.
/// </summary>
public class GuardianRarityTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    // Lit on Steam-19 from 11:04 on 28/11, after Vermilion's 5th evolution.
    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.GuardianRarity;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(12);

    // Rarities a guardian can climb in one run (each needs more evolutions, so one is the usual case).
    private const int MaxSteps = 4;

    public override IEnumerator Execute()
    {
        NextRunTime = DateTime.Now + RecheckDelay;

        var guardians = MagicQuartersScreen.Unlocked;
        if (guardians == null)
        {
            Debug("[INFO] Guardian rarity: the guardians didn't read from the game - not opening.");
            yield break;
        }

        var ready = guardians.Where(g => MagicQuartersScreen.CanRaiseRarity(g) == true).Select(g => g.guardianID).ToList();
        var state = string.Join(", ", guardians.Select(g =>
            $"{g.guardianName} {g.guardianRarity} (evolution {g.evolutionStage + 1}, max {g.GetMaxRarityThatCanUpgradeTo()}, next {MagicQuartersScreen.RarityCost(g)?.ToString() ?? "-"})"));
        Debug($"[INFO] Guardian rarity from the game: {state}; Contracts {MagicQuartersScreen.Contracts}.");
        if (ready.Count == 0)
        {
            Debug("[INFO] From the game: no guardian can raise its rarity - not opening.");
            yield break;
        }

        yield return TownScreen.Open;
        yield return TownScreen.OpenMagicQuarters;

        foreach (var square in MagicQuartersScreen.Squares().Where(s => s.guardian.IsUnlocked && ready.Contains(s.guardian.guardianID)))
        {
            var g = square.guardian;
            yield return new GameButton(transform: square.transform).Click();
            if (MagicQuartersScreen.RarityTab is { } tab) yield return tab.Click();
            yield return Poll.Until(() => MagicQuartersScreen.RarityBtn != null && MagicQuartersScreen.RarityGuardian == g.guardianID, 5);

            for (var step = 0; step < MaxSteps && MagicQuartersScreen.CanRaiseRarity(g) == true; step++)
            {
                var button = MagicQuartersScreen.RarityBtn;
                var icons = MagicQuartersScreen.RarityCostIcons;
                var (rarity, contracts, cost) = (g.guardianRarity, MagicQuartersScreen.Contracts, MagicQuartersScreen.RarityCost(g));

                // The price must be Contracts, on this guardian's screen; anything else (gems) is never paid.
                if (button == null || !button.IsClickable() || MagicQuartersScreen.RarityGuardian != g.guardianID ||
                    !icons.Any(i => i.Contains("contract", StringComparison.OrdinalIgnoreCase)))
                {
                    Debug($"[INFO] Guardian {g.guardianName}: rarity button {(button == null ? "missing" : button.IsClickable() ? "ready" : "not clickable")}, " +
                          $"screen on {MagicQuartersScreen.RarityGuardian?.ToString() ?? "?"}, icons [{string.Join(", ", icons)}] - nothing bought.");
                    break;
                }

                yield return button.Click();
                yield return Poll.Until(() => g.guardianRarity != rarity && MagicQuartersScreen.Contracts < contracts, 15);

                var raised = g.guardianRarity != rarity && MagicQuartersScreen.Contracts < contracts;
                Debug(raised
                    ? $"[INFO] Guardian {g.guardianName}: rarity {rarity} -> {g.guardianRarity} for {cost} Contracts ({contracts} -> {MagicQuartersScreen.Contracts})."
                    : $"[FAILED] Guardian {g.guardianName}: rarity clicked ({cost} Contracts) but neither the rarity nor the Contracts moved.");
                if (!raised) break;

                // The rarity-up animation (it may start a moment after the rarity moves) hides the close button.
                var waitFrom = DateTime.Now;
                yield return Poll.Until(() => MagicQuartersScreen.RarityAnimating, 2);
                yield return Poll.Until(() => !MagicQuartersScreen.RarityAnimating, 15);
                Debug($"[INFO] Guardian {g.guardianName}: rarity animation waited {(DateTime.Now - waitFrom).TotalSeconds:0.0} s.");
            }
        }

        yield return MagicQuartersScreen.CloseLockedPopup;
        yield return MagicQuartersScreen.Close;
        yield return TownScreen.Close;
    }
}
