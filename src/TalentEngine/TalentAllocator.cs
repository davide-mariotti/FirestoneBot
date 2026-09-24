using System;
using System.Collections.Generic;
using System.Linq;

namespace Firebot.TalentEngine;

/// <summary>
///     Decides how to spend available talent points to push as deep into the tree as possible, then
///     spend anything left over by priority. Stateless and pure - the caller (TalentsTask) supplies the
///     account's current snapshot every time; nothing here remembers a previous run.
///     Decision order, per call: (1) find the shallowest not-yet-open tier; if the points on hand can't
///     reach it, stop advancing and go straight to leftover spending; (2) otherwise fill the shortfall
///     from the highest-priority currently-open, uncapped, unlocked candidates first, spilling to the
///     next priority only once the previous one is capped - this exactly reproduces "prefer high
///     priority, give low priority only the strict minimum" without needing to compare specific
///     combinations, because reaching a tier only depends on the TOTAL spent, never on which specific
///     nodes hold it; (3) repeat, since crossing one tier may make the next one reachable with the same
///     points; (4) once no deeper tier is reachable, spend whatever is left the same way (highest
///     priority first) but without a target to stop at, so this phase maxes candidates out instead of
///     giving only the minimum.
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

    /// <summary>Spends up to `points` across candidates in the order given (already priority-sorted),
    ///     filling each one's remaining capacity before moving to the next. Mutates ranks/gains; returns
    ///     nothing since the caller doesn't need how much was actually placed (the depth phase already
    ///     checked total capacity is sufficient before calling; the leftover phase is fine spending
    ///     less than `points` if candidates run out first).</summary>
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

    /// <summary>Every node that's currently a legal investment target: its tier is already open, it has
    ///     spare capacity, it's not reactively known-locked, and its own prerequisite (if any) is met.
    ///     Sorted by priority descending, then by index for determinism.</summary>
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
