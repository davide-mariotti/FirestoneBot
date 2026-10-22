#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firebot.Tasks.Town;

/// <summary>Whether to buy today's amulet, and why - the reason goes into the log either way.</summary>
public sealed record AmuletDecision(bool Buy, int Tier, string Reason);

/// <summary>
///     Picks which daily amulets to pay 2000 gems for (AMULETS_PLAN.md). Pure: no game types, so the tests
///     project compiles it in directly. Codes are the game's AmuletCode names.
/// </summary>
public static class AmuletPlanner
{
    public const int Price = 2000;

    // Tier and the level the feature it boosts is used from. Order inside a tier is only how it reads:
    // one amulet a day, so it never decides between two.
    private static readonly (string Code, int Tier, int Level)[] Table =
    {
        ("AmuletOfConquest", 1, 1), ("AmuletOfWar", 1, 1), ("AmuletOfKnowledge", 1, 1), ("AmuletOfAlchemy", 1, 120),
        ("AmuletOfCartography", 2, 10), ("AmuletOfTheSky", 2, 1), ("AmuletOfExploration", 2, 10),
        ("AmuletOfLiberation", 2, 50), ("AmuletOfThePioneers", 2, 50), ("AmuletOfTheQuartermaster", 2, 50),
        ("AmuletOfInsight", 2, 200),
        ("AmuletOfAstrology", 3, 1), ("AmuletOfTheSeven", 3, 1), ("AmuletOfTinkering", 3, 50),
        ("AmuletOfProduction", 3, 50), ("AmuletOfClarity", 3, 50), ("AmuletOfGreed", 3, 10)
    };

    private static readonly string[] Never = { "AmuletOfMidas", "AmuletOfPower" };

    /// <summary>
    ///     Buys a tier-t amulet when the gems left after it still cover `reserve` plus one price for each
    ///     amulet still missing in the tiers above, at most t-1 of them: a tier 1 never waits, a tier 2
    ///     keeps one tier 1 covered, a tier 3 up to two.
    /// </summary>
    public static AmuletDecision Decide(string today, IReadOnlyCollection<string> owned, int level, double gems,
        double reserve)
    {
        if (Never.Contains(today)) return new AmuletDecision(false, 0, "never bought");

        var row = Array.Find(Table, r => r.Code == today);
        if (row.Code == null) return new AmuletDecision(false, 0, "not a gem amulet in the plan");
        if (owned.Contains(today)) return new AmuletDecision(false, row.Tier, "already owned");
        if (level < row.Level) return new AmuletDecision(false, row.Tier, $"useful from level {row.Level}");

        var missing = Table.Where(r => r.Tier < row.Tier && level >= r.Level && !owned.Contains(r.Code))
            .Select(r => r.Code).ToList();
        var held = Math.Min(missing.Count, row.Tier - 1);
        var needed = Price + reserve + Price * held;
        if (gems >= needed) return new AmuletDecision(true, row.Tier, $"gems {gems:0} cover {needed:0}");

        var why = held > 0 ? $" ({string.Join(", ", missing.Take(held))} missing)" : "";
        return new AmuletDecision(false, row.Tier, $"needs {needed:0}{why}");
    }
}
