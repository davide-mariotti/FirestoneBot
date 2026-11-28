using Firebot.Tasks.Guild;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// TreeOfLifePlanner is linked in from src/Tasks/Guild: it has no game dependencies.
public class TreeOfLifePlannerTests
{
    // The game's cost: 600 tokens for level 1, 100 more a level (1.700 at 11, seen on Steam-20..28).
    private static TreeNode Node(int level, bool priority = false, int cap = 27) => new(level, cap, priority, 600 + 100 * level);

    [Fact]
    public void Pick_PriorityFirst_ThenTheCheapest()
    {
        // Steam-27 on 27/11: the priorities at 15, the rest at 11-12, the tree allowing more -> a priority.
        Assert.Equal(1, TreeOfLifePlanner.Pick(new[] { Node(11), Node(15, true), Node(12) }));
        // The cheapest priority first.
        Assert.Equal(1, TreeOfLifePlanner.Pick(new[] { Node(16, true), Node(15, true) }));
        // Priorities at their cap: the cheapest of the rest.
        Assert.Equal(2, TreeOfLifePlanner.Pick(new[] { Node(27, true), Node(12), Node(11) }));
        // All at the cap means nothing to buy.
        Assert.Null(TreeOfLifePlanner.Pick(new[] { Node(27, true), Node(27) }));
        // Costs unknown (0): by level.
        Assert.Equal(1, TreeOfLifePlanner.Pick(new[] { new TreeNode(12, 27, false), new TreeNode(11, 27, false) }));
    }
}
