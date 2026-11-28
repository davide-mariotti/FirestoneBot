#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Firebot.Tasks.Guild;

/// <summary>
///     A Personal Tree node: its level, how far it can go now, whether it's a priority branch, and the
///     tokens its next level costs (0 when unknown).
/// </summary>
public sealed record TreeNode(int Level, int Cap, bool Priority, int Cost = 0);

/// <summary>
///     Which Personal Tree node to buy next. Pure: no game types, so the tests project compiles it in
///     directly.
/// </summary>
public static class TreeOfLifePlanner
{
    /// <summary>
    ///     As for Firestone research (27/11): the priority branches first, up to what the tree allows,
    ///     then the others - the cheapest first within each (the cost goes by level: 600 + 100 a level,
    ///     the same on every node). Null when every node is at its cap. The steps of 5 levels before
    ///     (04/10) held the priorities at 15 with the tree allowing more (Steam-27, 27/11).
    /// </summary>
    public static int? Pick(IReadOnlyList<TreeNode> nodes) =>
        Enumerable.Range(0, nodes.Count)
            .Where(i => nodes[i].Level < nodes[i].Cap)
            .OrderBy(i => nodes[i].Priority ? 0 : 1)
            .ThenBy(i => nodes[i].Cost)
            .ThenBy(i => nodes[i].Level)
            .Select(i => (int?)i)
            .FirstOrDefault();
}
