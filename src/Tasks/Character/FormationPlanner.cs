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

    /// <summary>
    ///     The deck cell whose portrait sprite is this hero's: "avatarBurt164(Clone)", a skin like
    ///     "murielTheNurseOfDeathAvatar164(Clone)", or the game's own misspelling "avatarValerious164". A
    ///     portrait containing the name wins; otherwise the only one within 2 letters of it. Null if none.
    /// </summary>
    public static int? MatchPortrait(string name, IReadOnlyList<string> portraits)
    {
        var key = name.ToLowerInvariant();
        for (var i = 0; i < portraits.Count; i++)
            if (portraits[i].ToLowerInvariant().Contains(key)) return i;

        static string Token(string p) => new(p.ToLowerInvariant().Replace("avatar", "").TakeWhile(char.IsLetter).ToArray());
        var near = Enumerable.Range(0, portraits.Count).Where(i => Distance(Token(portraits[i]), key) <= 2).ToList();
        return near.Count == 1 ? near[0] : null;
    }

    private static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
    }

    // log10(Σ 10^x), shifted by the largest term so it never overflows.
    private static double LogSum(IEnumerable<double> logs)
    {
        var list = logs.ToList();
        if (list.Count == 0) return double.NegativeInfinity;
        var max = list.Max();
        return max + Math.Log10(list.Sum(x => Math.Pow(10, x - max)));
    }
}
