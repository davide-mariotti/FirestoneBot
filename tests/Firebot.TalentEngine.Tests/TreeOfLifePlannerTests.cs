using Firebot.Tasks.Guild;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// TreeOfLifePlanner is linked in from src/Tasks/Guild: it has no game dependencies.
public class TreeOfLifePlannerTests
{
    private static TreeNode Node(int level, bool priority = false, int cap = 10) => new(level, cap, priority);

    [Fact]
    public void Pick_FollowsTheStepsOfFive_PriorityFirst()
    {
        // Steam-4 on 04/10: every node at 5, the tree allows 10 -> a priority branch first.
        Assert.Equal(1, TreeOfLifePlanner.Pick(new[] { Node(5), Node(5, true), Node(5) }));
        // Priority branches at 10, the others still at 5: the others catch up before anything goes to 15.
        Assert.Equal(2, TreeOfLifePlanner.Pick(new[] { Node(10, true, 20), Node(6), Node(5) }));
        // Everyone at 10: the priority branches open the step to 15.
        Assert.Equal(1, TreeOfLifePlanner.Pick(new[] { Node(10, cap: 20), Node(10, true, 20) }));
        // A node at its cap is skipped; all at the cap means nothing to buy.
        Assert.Equal(1, TreeOfLifePlanner.Pick(new[] { Node(10, true), Node(7) }));
        Assert.Null(TreeOfLifePlanner.Pick(new[] { Node(10, true), Node(10) }));
    }
}
