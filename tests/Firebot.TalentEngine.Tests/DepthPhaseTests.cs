using System.Collections.Generic;
using System.Linq;
using Firebot.TalentEngine;
using Xunit;

namespace Firebot.TalentEngine.Tests;

public class DepthPhaseTests
{
    // Tier 0 (threshold 0, always open): A, B, C. Tier 1 (threshold 10): D - only used to give the
    // depth phase something to chase; D itself is never touched by these tests.
    private static TalentTreeDefinition BuildAbcTree(int maxA = 6, int maxB = 6, int maxC = 6) =>
        new(
            new[]
            {
                new TalentNode(0, "A", maxA, Tier: 0),
                new TalentNode(1, "B", maxB, Tier: 0),
                new TalentNode(2, "C", maxC, Tier: 0),
                new TalentNode(3, "D", 1, Tier: 1)
            },
            new[] { 0, 10 });

    [Fact]
    public void FreshTree_FillsShallowestTierInPriorityOrder()
    {
        var tree = BuildAbcTree();
        var priorities = new Dictionary<string, int> { ["A"] = 100, ["B"] = 90, ["C"] = 0 };

        var steps = TalentAllocator.Plan(tree, new[] { 0, 0, 0, 0 }, new HashSet<int>(), 10, priorities);

        Assert.Equal(6, GainFor(steps, 0)); // A maxed
        Assert.Equal(4, GainFor(steps, 1)); // B gets the rest of the 10 needed
        Assert.Equal(0, GainFor(steps, 2)); // C untouched - not needed to reach the threshold
    }

    [Fact]
    public void PartiallyInvestedTree_OnlyTopsUpTheRemainingGap()
    {
        var tree = BuildAbcTree();
        var priorities = new Dictionary<string, int> { ["A"] = 100, ["B"] = 90, ["C"] = 0 };

        // C already has 2 points - only 8 new points are needed to reach the tier-1 threshold of 10.
        var steps = TalentAllocator.Plan(tree, new[] { 0, 0, 2, 0 }, new HashSet<int>(), 8, priorities);

        Assert.Equal(6, GainFor(steps, 0)); // A maxed
        Assert.Equal(2, GainFor(steps, 1)); // B tops up the remaining gap
        Assert.Equal(0, GainFor(steps, 2)); // C's existing 2 points are never touched or re-spent
    }

    [Fact]
    public void PriorityTalentWithInsufficientCap_SpillsToNextPriority()
    {
        var tree = BuildAbcTree(maxA: 3, maxB: 3, maxC: 6);
        var priorities = new Dictionary<string, int> { ["A"] = 100, ["B"] = 90, ["C"] = 0 };

        var steps = TalentAllocator.Plan(tree, new[] { 0, 0, 0, 0 }, new HashSet<int>(), 10, priorities);

        Assert.Equal(3, GainFor(steps, 0)); // A - capped at 3
        Assert.Equal(3, GainFor(steps, 1)); // B - capped at 3
        Assert.Equal(4, GainFor(steps, 2)); // C - takes the overflow neither A nor B could hold
    }

    [Fact]
    public void MultipleTiersReachableWithTheSamePoints_KeepsAdvancing()
    {
        // Tier 0 (threshold 0): X. Tier 1 (threshold 5): Y (higher priority than X). Tier 2
        // (threshold 15): Z. 15 available points should cross both thresholds in one call.
        var tree = new TalentTreeDefinition(
            new[]
            {
                new TalentNode(0, "X", 20, Tier: 0),
                new TalentNode(1, "Y", 20, Tier: 1),
                new TalentNode(2, "Z", 5, Tier: 2)
            },
            new[] { 0, 5, 15 });
        var priorities = new Dictionary<string, int> { ["X"] = 10, ["Y"] = 100, ["Z"] = 10 };

        var steps = TalentAllocator.Plan(tree, new[] { 0, 0, 0 }, new HashSet<int>(), 15, priorities);

        // Tier 1 isn't open yet when the first 5 points are placed, so only X can take them.
        Assert.Equal(5, GainFor(steps, 0));
        // Once tier 1 opens, Y (higher priority) takes the remaining 10 needed to cross tier 2.
        Assert.Equal(10, GainFor(steps, 1));
        // Tier 2 was only just reached - no points left to spend on Z.
        Assert.Equal(0, GainFor(steps, 2));
    }

    private static int GainFor(IReadOnlyList<TalentAllocationStep> steps, int nodeIndex) =>
        steps.FirstOrDefault(s => s.NodeIndex == nodeIndex)?.PointsToAdd ?? 0;
}
