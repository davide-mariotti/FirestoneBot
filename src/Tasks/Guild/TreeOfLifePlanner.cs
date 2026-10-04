#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Firebot.Tasks.Guild;

/// <summary>A Personal Tree node: its level, how far it can go now, and whether it's a priority branch.</summary>
public sealed record TreeNode(int Level, int Cap, bool Priority);

/// <summary>
///     Which Personal Tree node to buy next. Pure: no game types, so the tests project compiles it in
///     directly.
/// </summary>
public static class TreeOfLifePlanner
{
    private const int Step = 5;

    /// <summary>
    ///     The user's order (04/10): in steps of 5 levels - the priority branches up to the step, then
    ///     every other node, then the next step - lowest level first within each. Null when every node
    ///     is at its cap.
    /// </summary>
    public static int? Pick(IReadOnlyList<TreeNode> nodes)
    {
        var open = Enumerable.Range(0, nodes.Count).Where(i => nodes[i].Level < nodes[i].Cap).ToList();
        if (open.Count == 0) return null;

        var step = (open.Min(i => nodes[i].Level) / Step + 1) * Step;
        return open.Where(i => nodes[i].Level < step)
            .OrderBy(i => nodes[i].Priority ? 0 : 1)
            .ThenBy(i => nodes[i].Level)
            .First();
    }
}
