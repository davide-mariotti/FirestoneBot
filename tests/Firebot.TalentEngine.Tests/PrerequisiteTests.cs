using System.Collections.Generic;
using System.Linq;
using Firebot.TalentEngine;
using Xunit;

namespace Firebot.TalentEngine.Tests;

public class PrerequisiteTests
{
    [Fact]
    public void LockedNode_IsExcludedEvenWhenHighestPriority()
    {
        // Single tier (threshold 0) - no deeper tier to chase, so this goes straight to the leftover
        // phase. B has the higher priority but requires A to already hold >= 1 point.
        var tree = new TalentTreeDefinition(
            new[]
            {
                new TalentNode(0, "A", 5, Tier: 0),
                new TalentNode(1, "B", 5, Tier: 0, PrerequisiteIndex: 0, PrerequisiteMinRank: 1)
            },
            new[] { 0 });
        var priorities = new Dictionary<string, int> { ["A"] = 10, ["B"] = 100 };

        var steps = TalentAllocator.Plan(tree, new[] { 0, 0 }, new HashSet<int>(), 3, priorities);

        Assert.Contains(steps, s => s.NodeIndex == 0 && s.PointsToAdd == 3);
        Assert.DoesNotContain(steps, s => s.NodeIndex == 1);
    }

    [Fact]
    public void PrerequisiteAlreadySatisfied_NodeBecomesAvailable()
    {
        var tree = new TalentTreeDefinition(
            new[]
            {
                new TalentNode(0, "A", 5, Tier: 0),
                new TalentNode(1, "B", 5, Tier: 0, PrerequisiteIndex: 0, PrerequisiteMinRank: 1)
            },
            new[] { 0 });
        var priorities = new Dictionary<string, int> { ["A"] = 10, ["B"] = 100 };

        // A already has 1 point from earlier - B's prerequisite is satisfied from the start.
        var steps = TalentAllocator.Plan(tree, new[] { 1, 0 }, new HashSet<int>(), 3, priorities);

        Assert.Equal(3, steps.Single(s => s.NodeIndex == 1).PointsToAdd);
        Assert.DoesNotContain(steps, s => s.NodeIndex == 0);
    }

    [Fact]
    public void ReactivelyLockedNode_IsExcludedEvenThoughDataHasNoPrerequisite()
    {
        // No static PrerequisiteIndex on B - but the caller (the live task, discovering an unmapped
        // real-game lock) passes it in lockedNodeIndices, same effect.
        var tree = new TalentTreeDefinition(
            new[]
            {
                new TalentNode(0, "A", 5, Tier: 0),
                new TalentNode(1, "B", 5, Tier: 0)
            },
            new[] { 0 });
        var priorities = new Dictionary<string, int> { ["A"] = 10, ["B"] = 100 };

        var steps = TalentAllocator.Plan(tree, new[] { 0, 0 }, new HashSet<int> { 1 }, 3, priorities);

        Assert.Equal(3, steps.Single(s => s.NodeIndex == 0).PointsToAdd);
        Assert.DoesNotContain(steps, s => s.NodeIndex == 1);
    }
}
