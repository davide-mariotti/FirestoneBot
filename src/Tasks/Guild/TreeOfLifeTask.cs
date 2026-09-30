using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Spends Expedition Tokens on the Personal Tree. Each purchase goes to a priority upgrade when
///     one can be bought (TreeOfLife.PriorityUpgrades), else to the lowest-level upgrade - the
///     cheapest, since cost only depends on an upgrade's own level. Picking again after every
///     purchase spreads the tokens instead of maxing one upgrade while the rest sit at zero.
/// </summary>
public class TreeOfLifeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 10;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // A maxed node still reports clickable (it opens the preview), so the level is checked instead.
    private const int MaxUpgradeLevel = 5;

    private const int MaxIterations = 200;

    public override IEnumerator Execute()
    {
        yield return TownGuild.Open;
        yield return TownGuild.OpenTreeOfLife;
        yield return TreeOfLife.OpenPersonalTab;

        var unaffordableThisRun = new HashSet<int>();

        for (var i = 0; i < MaxIterations; i++)
        {
            var best = FindBestUpgrade(unaffordableThisRun);
            if (best == null) break;

            var name = TreeOfLife.PersonalUpgradeName(best.Value);
            var before = TreeOfLife.PersonalNodeLevel(best.Value);
            yield return TreeOfLife.PersonalNode(best.Value).Click();
            yield return TreeOfLife.ConfirmPurchase();

            if (!CurrencyMissingPopup.IsShowing)
            {
                Debug($"[INFO] Tree of Life: {name} {before} -> {TreeOfLife.PersonalNodeLevel(best.Value)}.");
                continue;
            }

            Debug($"[INFO] Tree of Life: {name} at {before} costs more tokens than are left.");

            // Too expensive now: skip just this node, so cheaper ones still get their turn.
            yield return CurrencyMissingPopup.Close;
            unaffordableThisRun.Add(best.Value);
        }

        yield return TreeOfLife.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private static int? FindBestUpgrade(ICollection<int> unaffordableThisRun)
    {
        int? bestPriority = null;
        var bestPriorityLevel = int.MaxValue;
        int? bestOther = null;
        var bestOtherLevel = int.MaxValue;

        for (var i = 0; i < TreeOfLife.PersonalUpgradeCount; i++)
        {
            if (unaffordableThisRun.Contains(i)) continue;
            if (!TreeOfLife.PersonalNode(i).IsClickable()) continue;

            var level = TreeOfLife.PersonalNodeLevel(i);
            if (level >= MaxUpgradeLevel) continue;

            if (TreeOfLife.IsPriority(i))
            {
                if (level >= bestPriorityLevel) continue;
                bestPriorityLevel = level;
                bestPriority = i;
            }
            else
            {
                if (level >= bestOtherLevel) continue;
                bestOtherLevel = level;
                bestOther = i;
            }
        }

        return bestPriority ?? bestOther;
    }
}
