using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Logger = Firebot.Core.Logger;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Spends Expedition Tokens on the Personal Tree, in the order TreeOfLifePlanner gives: steps of 5
///     levels, priority branches first (the user's rule, 04/10). Picking again after every purchase
///     spreads the tokens instead of maxing one upgrade while the rest sit at zero. War Machines
///     level with the same tokens and stay off (warmachinestask).
/// </summary>
public class TreeOfLifeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 10;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private const int MaxIterations = 200;

    public override IEnumerator Execute()
    {
        yield return Buy(MaxIterations);

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    /// <summary>
    ///     Up to `purchases` upgrades, in order, while the tokens last. Also an event challenge's
    ///     (EventChallengeActions). onBought gets how many were bought.
    /// </summary>
    public static IEnumerator Buy(int purchases, Action<int> onBought = null)
    {
        yield return TownGuild.Open;
        yield return TownGuild.OpenTreeOfLife;
        yield return TreeOfLife.OpenPersonalTab;

        var bought = 0;

        for (var i = 0; i < MaxIterations && bought < purchases; i++)
        {
            var best = TreeOfLifePlanner.Pick(TreeOfLife.PersonalNodes());
            if (best == null) break;

            var name = TreeOfLife.PersonalUpgradeName(best.Value);
            var before = TreeOfLife.PersonalNodeLevel(best.Value);
            yield return TreeOfLife.PersonalNode(best.Value).Click();
            yield return TreeOfLife.ConfirmPurchase();

            if (!CurrencyMissingPopup.IsShowing)
            {
                // The next pick reads the level again: wait for the purchase to land in it.
                yield return Poll.Until(() => TreeOfLife.PersonalNodeLevel(best.Value) != before);
                Logger.Debug($"[INFO] Tree of Life: {name} {before} -> {TreeOfLife.PersonalNodeLevel(best.Value)}.");
                bought++;
                continue;
            }

            // In order or not at all: the tokens wait for this node rather than go to a later one.
            Logger.Debug($"[INFO] Tree of Life: {name} at {before} costs more tokens than are left.");
            yield return CurrencyMissingPopup.Close;
            break;
        }

        onBought?.Invoke(bought);

        yield return TreeOfLife.Close;
        yield return TownGuild.Close;
    }
}
