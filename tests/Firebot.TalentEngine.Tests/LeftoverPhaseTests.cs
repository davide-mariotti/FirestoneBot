using System.Collections.Generic;
using System.Linq;
using Firebot.TalentEngine;
using Xunit;

namespace Firebot.TalentEngine.Tests;

public class LeftoverPhaseTests
{
    [Fact]
    public void PointsBeyondMaxDepth_AreSpentByPriorityInsteadOfLeftIdle()
    {
        // A single tier (threshold 0) - there is no deeper tier to ever chase, so every point goes
        // straight to the leftover phase.
        var tree = new TalentTreeDefinition(
            new[]
            {
                new TalentNode(0, "A", 5, Tier: 0),
                new TalentNode(1, "B", 5, Tier: 0)
            },
            new[] { 0 });
        var priorities = new Dictionary<string, int> { ["A"] = 100, ["B"] = 50 };

        // 7 points: more than A's cap of 5 alone, but within A+B's combined capacity of 10.
        var steps = TalentAllocator.Plan(tree, new[] { 0, 0 }, new HashSet<int>(), 7, priorities);

        Assert.Equal(5, steps.Single(s => s.NodeIndex == 0).PointsToAdd); // A maxed first
        Assert.Equal(2, steps.Single(s => s.NodeIndex == 1).PointsToAdd); // B takes the rest
    }

    [Fact]
    public void PointsBeyondEveryCandidatesCapacity_AreSimplyNotAllocated()
    {
        var tree = new TalentTreeDefinition(
            new[] { new TalentNode(0, "A", 5, Tier: 0) },
            new[] { 0 });
        var priorities = new Dictionary<string, int> { ["A"] = 100 };

        // Only 5 points can ever be placed - the allocator doesn't invent a place for the other 3.
        var steps = TalentAllocator.Plan(tree, new[] { 0 }, new HashSet<int>(), 8, priorities);

        Assert.Equal(5, steps.Single(s => s.NodeIndex == 0).PointsToAdd);
    }
}
