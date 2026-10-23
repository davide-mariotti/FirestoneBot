#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firebot.Tasks.Character;

/// <summary>
///     A hero as the planner sees it, all in log10: the game's attributes outgrow a double. Offense is
///     damage / attack speed × (1 + crit chance × crit damage), which matched the game's own DPS on
///     Steam-0 (23/10). Potential is the same from the hero's base damage times the account's
///     specialization, attack style and resource multipliers: what it would be with no investment of
///     its own, which is what the account's artifacts, beasts and trees favor.
/// </summary>
public sealed record HeroStats(string Name, bool Melee, double Offense, double Potential);

public sealed record Formation(IReadOnlyList<string> Slots, string Leader, double Score);

/// <summary>Pure: no game types, so the tests project compiles it in directly (FORMATION_PLAN.md).</summary>
public static class FormationPlanner
{
    public const int Size = 5;

    /// <summary>
    ///     The 5 heroes with the highest value, the leader the one that gains most from leading. Each
    ///     hero adds 10^value, the leader times leaderRatio (log10 of leadership / team bonus: 1.18 on
    ///     Steam-0). Above 0 the strongest leads; below it the weakest of the five, who loses the least.
    ///     Melee in front, ranged behind; inside each zone the most valuable last, where it's hit least.
    /// </summary>
    public static Formation Best(IEnumerable<HeroStats> heroes, Func<HeroStats, double> value, double leaderRatio)
    {
        var five = heroes.OrderByDescending(value).Take(Size).ToList();
        if (five.Count == 0) return new Formation(Array.Empty<string>(), "", double.NegativeInfinity);

        var leader = leaderRatio >= 0 ? five[0] : five[^1];
        double Worth(HeroStats h) => value(h) + (h == leader ? leaderRatio : 0);
        var slots = five.OrderBy(h => h.Melee ? 0 : 1).ThenBy(Worth).Select(h => h.Name).ToList();
        return new Formation(slots, leader.Name, LogSum(five.Select(Worth)));
    }

    /// <summary>The score of a given formation, to compare the one in battle with the best.</summary>
    public static double Score(IEnumerable<HeroStats> heroes, IEnumerable<string> names, string leader,
        Func<HeroStats, double> value, double leaderRatio)
    {
        var set = names.ToHashSet();
        return LogSum(heroes.Where(h => set.Contains(h.Name))
            .Select(h => value(h) + (h.Name == leader ? leaderRatio : 0)));
    }

    /// <summary>True when the new score beats the old one by minGain (0.10 = 10%), both in log10.</summary>
    public static bool Beats(double score, double current, double minGain) =>
        double.IsNegativeInfinity(current) || score - current >= Math.Log10(1 + minGain);

    // log10(Σ 10^x), shifted by the largest term so it never overflows.
    private static double LogSum(IEnumerable<double> logs)
    {
        var list = logs.ToList();
        if (list.Count == 0) return double.NegativeInfinity;
        var max = list.Max();
        return max + Math.Log10(list.Sum(x => Math.Pow(10, x - max)));
    }
}
