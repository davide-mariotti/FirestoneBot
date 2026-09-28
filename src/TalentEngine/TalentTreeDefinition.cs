using System;
using System.Collections.Generic;

namespace Firebot.TalentEngine;

/// <summary>
///     A talent tree's static shape: its nodes, and the total points that open each tier. The data is
///     hand-transcribed, so it's validated here - failing at load beats planning against a broken table.
/// </summary>
public sealed class TalentTreeDefinition
{
    public IReadOnlyList<TalentNode> Nodes { get; }

    /// <summary>TierThresholds[t]: points spent anywhere in the tree before tier t's nodes open.</summary>
    public IReadOnlyList<int> TierThresholds { get; }

    public TalentTreeDefinition(IReadOnlyList<TalentNode> nodes, IReadOnlyList<int> tierThresholds)
    {
        if (tierThresholds.Count == 0)
            throw new ArgumentException("A talent tree needs at least one tier.", nameof(tierThresholds));

        for (var t = 1; t < tierThresholds.Count; t++)
            if (tierThresholds[t] <= tierThresholds[t - 1])
                throw new ArgumentException(
                    $"Tier thresholds must be strictly increasing - tier {t} ({tierThresholds[t]}) " +
                    $"is not greater than tier {t - 1} ({tierThresholds[t - 1]}).", nameof(tierThresholds));

        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];

            if (node.Index != i)
                throw new ArgumentException($"Node at position {i} has Index {node.Index} - nodes must be " +
                                             "ordered and indexed 0..N-1.", nameof(nodes));

            if (node.MaxRank <= 0)
                throw new ArgumentException($"Node {i} ('{node.Name}') has MaxRank {node.MaxRank} - must be positive.",
                    nameof(nodes));

            if (node.Tier < 0 || node.Tier >= tierThresholds.Count)
                throw new ArgumentException($"Node {i} ('{node.Name}') has Tier {node.Tier}, outside " +
                                             $"[0, {tierThresholds.Count - 1}].", nameof(nodes));

            if (node.PrerequisiteIndex is { } prereq && (prereq < 0 || prereq >= i))
                throw new ArgumentException($"Node {i} ('{node.Name}')'s PrerequisiteIndex ({prereq}) must " +
                                             "refer to an earlier node.", nameof(nodes));
        }

        Nodes = nodes;
        TierThresholds = tierThresholds;
    }
}
