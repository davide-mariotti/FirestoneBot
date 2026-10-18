#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firebot.Tasks.Town;

/// <summary>
///     A blueprint level a machine can still buy: its specialization (the game's Specialization:
///     Damage 0, Tank 1, Healer 2) and attribute (UpgradeCode: Damage 0, Health 1, Armor 2).
/// </summary>
public sealed record BlueprintOption(string Code, string Name, int Role, int Attribute, int Level, double Cost, bool InSquad);

/// <summary>Picks the next blueprint level. Pure: no game types, so the tests project compiles it in directly.</summary>
public static class BlueprintPlanner
{
    // Attributes by role, most useful first. Tanks soak the hits: armor, then health. Damage
    // machines hit, and healers heal for a share of their own damage (wiki): damage, then health.
    private static readonly int[][] Order = { new[] { 0, 1, 2 }, new[] { 2, 1, 0 }, new[] { 0, 1, 2 } };

    /// <summary>
    ///     The campaign squad first: every machine's first attribute up to its cap, lowest level
    ///     first, then the second, then the third. Then the machines out of the squad, the same way:
    ///     every 5 levels on a machine is a +30% milestone to an account bonus (All attributes, Raining
    ///     gold, Prestigious: Steam-28, 19/10), whatever the attribute. Null when all are at their caps.
    /// </summary>
    public static BlueprintOption? Next(IEnumerable<BlueprintOption> open) =>
        open.OrderBy(o => o.InSquad ? 0 : 1).ThenBy(o => Array.IndexOf(Order[o.Role], o.Attribute))
            .ThenBy(o => o.Level).FirstOrDefault();
}
