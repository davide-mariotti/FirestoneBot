using System;
using System.Collections.Generic;
using System.Linq;

namespace Firebot.TalentEngine;

/// <summary>
///     Plans how to spend talent points, one point at a time, as deep into the tree as it goes. Pure -
///     the caller passes the live state on every call.
///     Tier t opens with TierThresholds[t] points spent anywhere and at least one point in tier t-1
///     (wiki). So when the last open tier has no point yet, the next point goes to its best node: the
///     one point that opens the way (Weaklings for Ancient Knowledge, Ancient Knowledge for Raining
///     Gold and Coworkers). Every other point goes to the open, uncapped node with the highest priority.
/// </summary>
public static class TalentAllocator
{
    /// <summary>The steps in the order to invest them: a node opening the way comes before what it opens.</summary>
    public static IReadOnlyList<TalentAllocationStep> Plan(
        TalentTreeDefinition tree,
        IReadOnlyList<int> currentRanks,
        IReadOnlySet<int> lockedNodeIndices,
        int availablePoints,
        IReadOnlyDictionary<string, int> priorities,
        int defaultPriority = 0)
    {
        if (currentRanks.Count != tree.Nodes.Count)
            throw new ArgumentException("currentRanks must have exactly one entry per node.", nameof(currentRanks));

        var ranks = currentRanks.ToArray();
        var order = new List<int>();
        var gains = new Dictionary<int, int>();

        for (var left = availablePoints; left > 0; left--)
        {
            var node = NextPoint(tree, ranks, lockedNodeIndices, priorities, defaultPriority);
            if (node == null) break;

            ranks[node.Value]++;
            if (!gains.ContainsKey(node.Value)) order.Add(node.Value);
            gains[node.Value] = gains.GetValueOrDefault(node.Value) + 1;
        }

        return order.Select(i => new TalentAllocationStep(i, gains[i])).ToList();
    }

    /// <summary>How many tiers are open, from the top: tier t needs its threshold and a point in t-1.</summary>
    public static int OpenTiers(TalentTreeDefinition tree, IReadOnlyList<int> ranks)
    {
        var spent = ranks.Sum();
        var open = 1;
        while (open < tree.TierThresholds.Count && tree.TierThresholds[open] <= spent && TierHasPoint(tree, ranks, open - 1))
            open++;
        return open;
    }

    private static int? NextPoint(TalentTreeDefinition tree, int[] ranks, IReadOnlySet<int> locked,
        IReadOnlyDictionary<string, int> priorities, int defaultPriority)
    {
        var open = OpenTiers(tree, ranks);
        var candidates = Enumerable.Range(0, tree.Nodes.Count)
            .Where(i => tree.Nodes[i].Tier < open && ranks[i] < tree.Nodes[i].MaxRank && !locked.Contains(i))
            .OrderByDescending(i => priorities.GetValueOrDefault(tree.Nodes[i].Name, defaultPriority))
            .ThenBy(i => i)
            .ToList();

        if (open < tree.TierThresholds.Count && !TierHasPoint(tree, ranks, open - 1))
            foreach (var i in candidates)
                if (tree.Nodes[i].Tier == open - 1) return i;

        return candidates.Count > 0 ? candidates[0] : null;
    }

    private static bool TierHasPoint(TalentTreeDefinition tree, IReadOnlyList<int> ranks, int tier)
    {
        for (var i = 0; i < tree.Nodes.Count; i++)
            if (tree.Nodes[i].Tier == tier && ranks[i] > 0) return true;
        return false;
    }
}
