using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Library.MeteoriteResearch;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;
using Logger = Firebot.Core.Logger;
using Library = Firebot.GameModel.Features.Town.Library.Library;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Meteorite Research: 5 trees of 13 nodes, each research paid in Meteorites. Meteorites also pay
///     for hero gear tier unlocks, so this never researches below min_meteorite_reserve.
/// </summary>
public class MeteoriteResearchTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    // Up to MaxNodesPerRun scans of ~30 s each, plus the purchases: past the global 120 s.
    internal override float? MaxRuntimeSeconds => 900f;

    private const int TreeCount = 5;
    private const int NodeCount = 13;

    private MelonPreferences_Entry<int> _recheckIntervalMinutes;
    private MelonPreferences_Entry<int> _minMeteoriteReserve;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_recheckIntervalMinutes != null) return;

        _recheckIntervalMinutes = category.CreateEntry(
            "recheck_interval_minutes",
            60,
            "Recheck Interval (minutes)",
            "How long to wait before checking again when the cheapest available research still " +
            "isn't affordable, or when the reserve threshold below is blocking research. Default: 60."
        );

        _minMeteoriteReserve = category.CreateEntry(
            "min_meteorite_reserve",
            3000,
            "Minimum Meteorite Reserve",
            "Never research below this Meteorite balance, so there's always a margin left for hero " +
            "gear tier unlocks (Meteorites are a currency shared between the two, per the wiki). " +
            "Default: 3000. Set to 0 to disable."
        );
    }

    // Large balances show abbreviated ("12.5K").
    private static GameText MeteoriteBalanceTxt => new(Paths.MenusLoc.LibraryLoc.MeteoriteBalanceTxt);

    public override IEnumerator Execute()
    {
        yield return Notifications.MeteoriteResearch;

        yield return TownScreen.Open;
        yield return TownScreen.OpenLibrary;
        yield return Library.OpenMeteoriteResearchTab;

        var minReserve = _minMeteoriteReserve?.Value ?? 3000;
        var balance = MeteoriteBalanceTxt.GetParsedDoubleAbbreviated();
        Debug($"[INFO] Meteorite balance {balance} ('{MeteoriteBalanceTxt.GetParsedText()}'), reserve {minReserve}.");
        if (minReserve > 0 && balance < minReserve)
            Debug("[INFO] Below the reserve - skipping research this run.");

        // Node after node: a round that buys nothing (reserve reached, nothing affordable) ends it.
        // ponytail: a new round scans forward from the tree the last one stopped on, so an earlier
        // tree waits for the next run; rescan from tree 1 if that ever leaves meteorites idle.
        for (var round = 0; round < MaxNodesPerRun && (minReserve <= 0 || balance >= minReserve); round++)
        {
            yield return RunResearch(minReserve, MaxLevelsPerRun);

            var after = MeteoriteBalanceTxt.GetParsedDoubleAbbreviated();
            if (after >= balance) break;
            balance = after;
        }

        NextRunTime = DateTime.Now + TimeSpan.FromMinutes(_recheckIntervalMinutes?.Value ?? 60);

        yield return Library.Close;
        yield return TownScreen.Close;
    }

    /// <summary>
    ///     An event challenge's researches (EventChallengeActions): up to `levels` levels of the node a
    ///     normal run would pick, with no reserve kept. onResearched gets how many were bought.
    /// </summary>
    public static IEnumerator Research(int levels, Action<int> onResearched)
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenLibrary;
        yield return Library.OpenMeteoriteResearchTab;

        yield return RunResearch(0, levels, onResearched);

        yield return Library.Close;
        yield return TownScreen.Close;
    }

    // Best first, like FirestoneResearchTask's. These trees have no time-reduction nodes.
    private static readonly string[] PriorityTerms = { "Raining Gold", "Firestone Finder", "Firestone Effect" };

    // Index into PriorityTerms (lower is better), int.MaxValue for a non-priority node.
    private static int PriorityRank(string name)
    {
        for (var i = 0; i < PriorityTerms.Length; i++)
            if (name.Contains(PriorityTerms[i], StringComparison.OrdinalIgnoreCase))
                return i;

        return int.MaxValue;
    }

    /// <summary>
    ///     Researches one node: the best-ranked priority node, else the first unlocked one showing a cost.
    ///     Ranking applies within a tree - a tree that offers a priority node ends the scan, since
    ///     chasing a better one into the next tree costs a full 13-node sweep. An unaffordable pick is
    ///     a no-op click.
    /// </summary>
    // Safety bounds on one run's purchases, not pacing choices: the reserve is what stops it.
    private const int MaxLevelsPerRun = 30;
    private const int MaxNodesPerRun = 10;

    private static IEnumerator RunResearch(int minReserve, int maxLevels, Action<int> onResearched = null)
    {
        var node = new MeteoriteNode();

        int? bestPriorityIndex = null;
        int? bestPriorityTreeOffset = null;
        var bestPriorityRank = int.MaxValue;
        int? fallbackIndex = null;
        int? fallbackTreeOffset = null;

        var treeOffset = 0;
        while (treeOffset < TreeCount && bestPriorityRank > 0)
        {
            for (var index = 0; index < NodeCount; index++)
            {
                yield return node.Select(index);

                if (MeteoriteResearchPreview.IsUnlocked)
                {
                    var cost = MeteoriteResearchPreview.Cost;

                    // No cost shown (e.g. a maxed node) or unparsable: not a candidate.
                    if (cost > 0)
                    {
                        var rank = PriorityRank(MeteoriteResearchPreview.Name);

                        if (rank < bestPriorityRank)
                        {
                            bestPriorityRank = rank;
                            bestPriorityIndex = index;
                            bestPriorityTreeOffset = treeOffset;

                            // Nothing outranks rank 0 - stop instead of scanning the rest of the tree.
                            if (rank == 0)
                            {
                                yield return MeteoriteResearchPreview.Close;
                                break;
                            }
                        }

                        if (fallbackIndex == null && rank == int.MaxValue)
                        {
                            fallbackIndex = index;
                            fallbackTreeOffset = treeOffset;
                        }
                    }
                }

                yield return MeteoriteResearchPreview.Close;
            }

            if (bestPriorityIndex != null) break;

            if (treeOffset < TreeCount - 1)
            {
                var beforeTree = node.CurrentTreeName;
                yield return node.NextTree;

                if (node.CurrentTreeName == beforeTree)
                {
                    // Refused: the next tree is locked. Close just the toast, as in FirestoneResearchTask.
                    yield return new GameButton(Paths.MenusLoc.GenericMessageLoc.CloseBtn).Click();
                    break;
                }
            }

            treeOffset++;
        }

        var bestIndex = bestPriorityIndex ?? fallbackIndex;
        var bestTreeOffset = bestPriorityIndex != null ? bestPriorityTreeOffset : fallbackTreeOffset;

        if (bestIndex == null) yield break;

        // Walk back from wherever the scan stopped to the picked node's tree.
        var lastReachedTree = Math.Min(treeOffset, TreeCount - 1);
        for (var back = lastReachedTree; back > bestTreeOffset; back--)
            yield return node.PreviousTree;

        Logger.Debug($"[INFO] Selected meteorite research node #{bestIndex} on tree offset {bestTreeOffset} " +
              $"(priorityRank={(bestPriorityRank == int.MaxValue ? "none" : bestPriorityRank.ToString())}). " +
              "Attempting - safe no-op if not yet affordable.");

        // Level after level of the picked node while the balance stays above the reserve after paying:
        // one level an hour left 9,000-14,000 Meteorites idle on several accounts (30/09). Each level
        // counts only once the balance has dropped. The node is clicked again for every level, so the
        // preview shows its next cost whether or not a purchase closes it; a maxed node shows none.
        var researched = 0;
        for (; researched < maxLevels; researched++)
        {
            yield return node.Select(bestIndex.Value);

            var cost = MeteoriteResearchPreview.Cost;
            var before = MeteoriteBalanceTxt.GetParsedDoubleAbbreviated();
            if (!MeteoriteResearchPreview.IsUnlocked || cost <= 0 || before - cost < minReserve) break;

            yield return MeteoriteResearchPreview.Research;
            yield return Poll.Until(() => MeteoriteBalanceTxt.GetParsedDoubleAbbreviated() < before);

            var after = MeteoriteBalanceTxt.GetParsedDoubleAbbreviated();
            Logger.Debug($"[INFO] Meteorite research '{MeteoriteResearchPreview.Name}' for {cost}: balance {before} -> {after}.");
            if (after >= before) break;
        }

        onResearched?.Invoke(researched);

        yield return MeteoriteResearchPreview.Close;
    }
}
