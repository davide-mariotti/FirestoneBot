#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firebot.Tasks.Town;

/// <summary>A guardian, Evolution counted as the wiki does: 1 before the first evolution.</summary>
public sealed record GuardianState(string Name, bool Unlocked, int Evolution, int Level, bool ReadyToEvolve);

/// <summary>Picks the guardian to train. Pure: no game types, so the tests project compiles it in directly.</summary>
public static class GuardianPlanner
{
    // guardian_index's order too. Vermilion's and Grace's evolution 5 unlock Ankaa, the Personal
    // Tree's level 50 Azhar (wiki); Azhar joins whichever round is on.
    public static readonly string[] Order = { "Vermilion", "Grace", "Ankaa", "Azhar" };

    // Every unlocked guardian to evolution 5 in that order, then every one to 10.
    private static readonly int[] Goals = { 5, 10 };

    /// <summary>
    ///     The first unlocked guardian in Order below the goal. One ready to evolve can't train (wiki),
    ///     so the next one trains until GuardianEvolutionTask evolves it. Null once all are past 10.
    /// </summary>
    public static GuardianState? Pick(IEnumerable<GuardianState> guardians)
    {
        var ordered = guardians.OrderBy(g => Array.IndexOf(Order, g.Name) is var i and >= 0 ? i : Order.Length).ToList();
        foreach (var goal in Goals)
            foreach (var g in ordered)
                if (g.Unlocked && !g.ReadyToEvolve && g.Evolution < goal) return g;
        return null;
    }
}
