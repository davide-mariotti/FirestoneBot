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
///     Spends Expedition Tokens on the Personal Tree, in the order TreeOfLifePlanner gives: priority
///     branches first, the cheapest first. Picking again after every purchase spreads the tokens. War
///     Machines level with the same tokens and wait for this tree to reach level 50 (WarMachinesTask).
/// </summary>
public class TreeOfLifeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 10;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // Tokens come in with every expedition (30-100 at a time), so the game's balance is checked often:
    // reading it costs nothing, while the 6 h recheck left them idle past the cost (Steam-27, 27/11).
    private static readonly TimeSpan DataRecheck = TimeSpan.FromMinutes(30);

    private const int MaxIterations = 200;

    public override IEnumerator Execute()
    {
        if (TreeOfLife.NextFromGame is { } next && GameState.ExpeditionTokens is { } tokens && tokens < next.Cost)
        {
            Debug($"[INFO] From the game: {tokens:0} tokens, next {next.Name} at {next.Level} costs " +
                  $"{(next.Cost == int.MaxValue ? "- (all at the cap)" : next.Cost.ToString())} - not opening.");
            NextRunTime = DateTime.Now + DataRecheck;
            yield break;
        }

        var bought = 0;
        yield return Buy(MaxIterations, n => bought = n);

        // A run that bought nothing although the data said it could (a locked node, say) waits long.
        NextRunTime = DateTime.Now + (bought > 0 && TreeOfLife.NextFromGame != null ? DataRecheck : RecheckDelay);
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
        var tokensBefore = TreeOfLife.Tokens;
        Logger.Debug($"[INFO] Tree of Life: {TreeOfLife.DescribeFromGame}");

        for (var i = 0; i < MaxIterations && bought < purchases; i++)
        {
            var nodes = TreeOfLife.PersonalNodes();
            var best = TreeOfLifePlanner.Pick(nodes);
            if (best == null) break;

            var name = TreeOfLife.PersonalUpgradeName(best.Value);
            var before = TreeOfLife.PersonalNodeLevel(best.Value);
            var cost = nodes[best.Value].Cost;
            if (i == 0) GameState.Compare("personal tree next cost", TreeOfLife.NextFromGame?.Cost.ToString(), cost.ToString());

            // In order or not at all: the tokens wait for this node rather than go to a later one.
            if (cost > 0 && GameState.ExpeditionTokens is { } tokens && tokens < cost)
            {
                Logger.Debug($"[INFO] Tree of Life: {name} at {before} costs {cost}, {tokens:0} tokens left.");
                break;
            }

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

            Logger.Debug($"[INFO] Tree of Life: {name} at {before} costs more tokens than are left.");
            yield return CurrencyMissingPopup.Close;
            break;
        }

        // The balance on every run measures the daily token income (04/10, the user's question).
        Logger.Debug($"[INFO] Tree of Life: tokens {tokensBefore} -> {TreeOfLife.Tokens}, {bought} bought.");
        onBought?.Invoke(bought);

        yield return TreeOfLife.Close;
        yield return TownGuild.Close;
    }
}
