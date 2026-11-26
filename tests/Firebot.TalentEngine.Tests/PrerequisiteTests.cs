using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Features.Character;
using Xunit;
using Xunit.Abstractions;

namespace Firebot.TalentEngine.Tests;

// The game's rule (wiki): tier t also needs a point in tier t-1.
public class PrerequisiteTests
{
    private readonly ITestOutputHelper _output;

    public PrerequisiteTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void LastOpenTierWithoutPoints_GetsTheOpeningPointFirst()
    {
        // Tier 0: A (high priority). Tier 1: B, the low-priority gate. Tier 2: C, the prize.
        var tree = new TalentTreeDefinition(
            new[] { new TalentNode(0, "A", 50, 0), new TalentNode(1, "B", 25, 1), new TalentNode(2, "C", 25, 2) },
            new[] { 0, 3, 5 });
        var priorities = new Dictionary<string, int> { ["A"] = 50, ["B"] = 0, ["C"] = 100 };

        // A holds 10: tiers 0-1 open, tier 2's threshold met but B has no point.
        var steps = TalentAllocator.Plan(tree, new[] { 10, 0, 0 }, new HashSet<int>(), 3, priorities);

        Assert.Equal(new[] { (1, 1), (2, 2) }, steps.Select(s => (s.NodeIndex, s.PointsToAdd)));
    }

    [Fact]
    public void ReactivelyLockedNode_IsExcludedEvenWhenHighestPriority()
    {
        var tree = new TalentTreeDefinition(new[] { new TalentNode(0, "A", 5, 0), new TalentNode(1, "B", 5, 0) }, new[] { 0 });
        var priorities = new Dictionary<string, int> { ["A"] = 10, ["B"] = 100 };

        var steps = TalentAllocator.Plan(tree, new[] { 0, 0 }, new HashSet<int> { 1 }, 3, priorities);

        Assert.Equal(3, steps.Single(s => s.NodeIndex == 0).PointsToAdd);
        Assert.DoesNotContain(steps, s => s.NodeIndex == 1);
    }

    [Fact]
    public void Steam20_OpensAncientKnowledgeThenTheChain()
    {
        // Steam-20 on 26/11: 213 spent, tier 9 (Weaklings, Expose Weakness) at 0, so Ancient Knowledge locked.
        var tree = TalentTreeData.Tree;
        // Tiers 1-7 full but Attack speed and Dodge at 0, Critical damage 1, Critical chance 8; Powerless enemy 12.
        var ranks = new int[tree.Nodes.Count];
        for (var i = 0; i <= 13; i++) ranks[i] = tree.Nodes[i].MaxRank;
        ranks[5] = ranks[9] = 0;
        ranks[10] = 1;
        ranks[7] = 8;
        ranks[14] = 12;
        Assert.Equal(213, ranks.Sum());
        var priorities = TalentBuildConfig.Resolve("", _ => { });

        var names = Names(tree, TalentAllocator.Plan(tree, ranks, new HashSet<int>(), 8, priorities));

        // One point opens each tier, so 8 points reach Alchemy (tier 17, 160 points).
        Assert.Equal(new[] { "Weaklings", "Ancient Knowledge", "Coworkers", "Twin dragons", "Attack speed", "Battle cry",
            "Critical damage", "Powerless enemy" }, names);
    }

    [Fact]
    public void AfterAReset_214Points_FollowThePriorities()
    {
        var tree = TalentTreeData.Tree;
        var priorities = TalentBuildConfig.Resolve("", _ => { });
        var steps = TalentAllocator.Plan(tree, new int[tree.Nodes.Count], new HashSet<int>(), 214, priorities);
        var ranks = new int[tree.Nodes.Count];
        foreach (var s in steps) ranks[s.NodeIndex] += s.PointsToAdd;
        foreach (var s in steps) _output.WriteLine($"[{s.NodeIndex}] {tree.Nodes[s.NodeIndex].Name} +{s.PointsToAdd}");

        Assert.Equal(214, ranks.Sum());
        Assert.Equal(19, TalentAllocator.OpenTiers(tree, ranks)); // through tier 19 (200 points)
        Assert.Equal(25, ranks[11]); // Librarian
        Assert.Equal(25, ranks[29]); // Alchemy
        Assert.Equal(10, ranks[21]); // Twin dragons
        Assert.Equal(15, ranks[24]); // Battle cry
        Assert.Equal(1, ranks[18]); // Ancient Knowledge: only the point that opens the way
        Assert.True(ranks[14] + ranks[15] <= 1 && ranks[27] + ranks[28] <= 1); // Powerless tiers: one point each
    }

    private static string[] Names(TalentTreeDefinition tree, IEnumerable<TalentAllocationStep> steps) =>
        steps.Select(s => tree.Nodes[s.NodeIndex].Name).ToArray();
}
