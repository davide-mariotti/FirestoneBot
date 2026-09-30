using System;
using System.Collections;
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

        // Cost only depends on the level, so once a node at level L is too expensive, every node at L
        // or above is too: only lower ones are still worth a try. Trying them one by one cost 4 s
        // each, 40-50 s a run with nothing left to buy (30/09).
        var affordableBelow = MaxUpgradeLevel;

        for (var i = 0; i < MaxIterations; i++)
        {
            var best = FindBestUpgrade(affordableBelow);
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

            yield return CurrencyMissingPopup.Close;
            affordableBelow = before;
        }

        yield return TreeOfLife.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private static int? FindBestUpgrade(int affordableBelow)
    {
        int? bestPriority = null;
        var bestPriorityLevel = int.MaxValue;
        int? bestOther = null;
        var bestOtherLevel = int.MaxValue;

        for (var i = 0; i < TreeOfLife.PersonalUpgradeCount; i++)
        {
            if (!TreeOfLife.PersonalNode(i).IsClickable()) continue;

            var level = TreeOfLife.PersonalNodeLevel(i);
            if (level >= affordableBelow) continue;

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
