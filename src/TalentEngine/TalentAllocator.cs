using System;
using System.Collections.Generic;
using System.Linq;

namespace Firebot.TalentEngine;

/// <summary>
///     Plans how to spend talent points: reach the deepest tier possible, then spend what's left. Pure -
///     the caller passes the live state on every call.
///     Each step closes the gap to the next tier with the open, uncapped, unlocked nodes, highest
///     priority first, moving on only once a node is capped. Opening a tier depends only on the total
///     spent, not on which nodes hold it, so this reaches the same depth as any other split while
///     giving low-priority nodes only the strict minimum. When no deeper tier is reachable, the
///     leftover points go the same way, maxing nodes out by priority.
/// </summary>
public static class TalentAllocator
{
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
        var remaining = availablePoints;
        var totalSpent = ranks.Sum();
        var gains = new Dictionary<int, int>();

        while (remaining > 0)
        {
            var nextTier = FindNextTier(tree, totalSpent);
            if (nextTier == null) break;

            var missing = tree.TierThresholds[nextTier.Value] - totalSpent;
            if (missing > remaining) break;

            var candidates = GetCandidates(tree, ranks, lockedNodeIndices, totalSpent, priorities, defaultPriority);
            var totalCapacity = candidates.Sum(i => tree.Nodes[i].MaxRank - ranks[i]);
            if (totalCapacity < missing) break;

            Fill(tree, ranks, gains, candidates, missing);
            totalSpent += missing;
            remaining -= missing;
        }

        if (remaining > 0)
        {
            var candidates = GetCandidates(tree, ranks, lockedNodeIndices, totalSpent, priorities, defaultPriority);
            Fill(tree, ranks, gains, candidates, remaining);
        }

        return gains
            .Select(kv => new TalentAllocationStep(kv.Key, kv.Value))
            .OrderBy(s => s.NodeIndex)
            .ToList();
    }

    /// <summary>
    ///     Spends up to points on the candidates in order, filling each before the next. Placing fewer
    ///     is fine: the depth phase checked the capacity first, and the leftover phase can run out.
    /// </summary>
    private static void Fill(
        TalentTreeDefinition tree, int[] ranks, Dictionary<int, int> gains, List<int> candidates, int points)
    {
        var toFill = points;
        foreach (var idx in candidates)
        {
            if (toFill <= 0) break;

            var capacity = tree.Nodes[idx].MaxRank - ranks[idx];
            var take = Math.Min(capacity, toFill);
            if (take <= 0) continue;

            ranks[idx] += take;
            gains[idx] = gains.GetValueOrDefault(idx) + take;
            toFill -= take;
        }
    }

    private static int? FindNextTier(TalentTreeDefinition tree, int totalSpent)
    {
        for (var t = 0; t < tree.TierThresholds.Count; t++)
            if (tree.TierThresholds[t] > totalSpent)
                return t;

        return null;
    }

    /// <summary>
    ///     The nodes that can take a point now - tier open, not capped, not known locked, prerequisite
    ///     met - highest priority first, then by index.
    /// </summary>
    private static List<int> GetCandidates(
        TalentTreeDefinition tree, int[] ranks, IReadOnlySet<int> lockedNodeIndices,
        int totalSpent, IReadOnlyDictionary<string, int> priorities, int defaultPriority)
    {
        var candidates = new List<int>();

        for (var i = 0; i < tree.Nodes.Count; i++)
        {
            var node = tree.Nodes[i];
            if (tree.TierThresholds[node.Tier] > totalSpent) continue;
            if (ranks[i] >= node.MaxRank) continue;
            if (lockedNodeIndices.Contains(i)) continue;
            if (node.PrerequisiteIndex is { } prereq && ranks[prereq] < node.PrerequisiteMinRank) continue;

            candidates.Add(i);
        }

        return candidates
            .OrderByDescending(i => priorities.GetValueOrDefault(tree.Nodes[i].Name, defaultPriority))
            .ThenBy(i => i)
            .ToList();
    }
}
