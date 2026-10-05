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

    // A scan of up to 5 trees, ~30 s each, plus the purchases: past the global 120 s.
    internal override float? MaxRuntimeSeconds => 900f;

    private static readonly TimeSpan NextNodeDelay = TimeSpan.FromMinutes(1);

    // The game re-enables research 5 s after a purchase (reenableCompleteMeteoriteResearchTime).
    private static readonly UnityEngine.WaitForSeconds ReenableWait = new(6f);

    private const int TreeCount = 5;
    private const int NodeCount = 13;
    private const string FirstTreeName = "tree1";

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

        // One node per run, the Library reopened between nodes: after a purchase a second round in the
        // same session read the stale, closed preview 13 times and stopped with 4,900 Meteorites to
        // spend (01/10, Steam-16). A run that bought something comes back a minute later for the next.
        var bought = false;
        if (minReserve <= 0 || balance >= minReserve)
        {
            yield return RunResearch(minReserve, MaxLevelsPerRun);
            bought = MeteoriteBalanceTxt.GetParsedDoubleAbbreviated() < balance;
        }

        NextRunTime = DateTime.Now + (bought ? NextNodeDelay : TimeSpan.FromMinutes(_recheckIntervalMinutes?.Value ?? 60));

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

    // A safety bound on one run's purchases, not a pacing choice: the reserve is what stops it.
    private const int MaxLevelsPerRun = 30;

    /// <summary>
    ///     Researches one node: the best-ranked priority node, else the cheapest unlocked one showing a cost.
    ///     Ranking applies within a tree - a tree that offers a priority node ends the scan, since
    ///     chasing a better one into the next tree costs a full 13-node sweep. An unaffordable pick is
    ///     a no-op click.
    /// </summary>
    private static IEnumerator RunResearch(int minReserve, int maxLevels, Action<int> onResearched = null)
    {
        var node = new MeteoriteNode();

        int? bestPriorityIndex = null;
        int? bestPriorityTreeOffset = null;
        var bestPriorityRank = int.MaxValue;
        int? fallbackIndex = null;
        int? fallbackTreeOffset = null;
        var fallbackCost = double.MaxValue;

        // The carousel opens on the tree last viewed, and the scan only moves forward: earlier trees
        // were never seen, and only a session's first run found a node (01/10: Steam-0 bought on 2
        // runs of 12, Meteorites piling up to 14,000 on Steam-12..15). Start from the first tree.
        for (var i = 0; i < TreeCount && node.CurrentTreeName != FirstTreeName; i++)
            yield return node.PreviousTree;

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

                        // The cheapest, not the first: on Steam-0 the first was 800 with 450 to spend
                        // above the reserve, next to nodes at 500 and 600.
                        if (rank == int.MaxValue && cost < fallbackCost)
                        {
                            fallbackCost = cost;
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
        // counts only once the balance has dropped. The levels are bought from the open preview, and
        // after the last one nothing is touched for ReenableWait: re-clicking the node and closing the
        // Library within a second or two of a purchase, which no player does, left the game unable to
        // open any preview again until a restart (01/10, Steam-16: reproduced with one level bought).
        yield return node.Select(bestIndex.Value);

        var researched = 0;
        for (; researched < maxLevels; researched++)
        {
            var cost = MeteoriteResearchPreview.Cost;
            var before = MeteoriteBalanceTxt.GetParsedDoubleAbbreviated();
            if (!MeteoriteResearchPreview.IsUnlocked || cost <= 0 || before - cost < minReserve) break;

            yield return MeteoriteResearchPreview.Research;
            yield return Poll.Until(() => MeteoriteBalanceTxt.GetParsedDoubleAbbreviated() < before);

            var after = MeteoriteBalanceTxt.GetParsedDoubleAbbreviated();
            Logger.Debug($"[INFO] Meteorite research '{MeteoriteResearchPreview.Name}' for {cost}: balance {before} -> {after}.");
            if (after >= before) break;
        }

        if (researched > 0) yield return ReenableWait;

        onResearched?.Invoke(researched);

        yield return MeteoriteResearchPreview.Close;
    }
}
