using System;
using System.Collections.Generic;
using Firebot.Core;

namespace Firebot.GameModel.Features.Character;

/// <summary>
///     Default talent priorities for a generic progression build, keyed by the real catalog name (see
///     TalentTreeData) - a single priority applies to every tier where that name repeats, since the
///     account can't tell which specific instance it "meant" and doesn't need to (the allocator only
///     needs relative ordering among whatever is currently open). Overridable per the cfg's
///     priority_overrides entry (TalentsTask.OnConfigure).
///     Per the user (2026-09-24): three of their originally-given names don't exist as literal talents
///     - "Leader - Auto Abilities" (matches the real catalog exactly, kept as-is) and "Party - Auto
///     Abilities" (aliased below to the real name, "Auto attacks" - the tree's other 1-point "auto"
///     talent) were a wording mismatch; "Damage"/"Health"/"Armor" were meant as informal categories
///     ("whatever talent boosts that stat"), not literal names - aliased below to the closest real
///     talents (the three role-specialization nodes), which the user can retune via priority_overrides
///     if this guess doesn't match what they actually meant. "Projectiles" and "Attribute Armor" (same
///     priority tier as Fist Fight/Precision/Magic Spells) have no real talent left unmatched once
///     those three are already covered by their own literal entries below, so they're dropped rather
///     than guessed at.
/// </summary>
public static class TalentBuildConfig
{
    public const int DefaultPriority = 0;

    public static readonly IReadOnlyDictionary<string, int> DefaultPriorities = new Dictionary<string, int>
    {
        ["Librarian"] = 100,
        ["Alchemy"] = 95,
        ["Twin dragons"] = 90,
        ["Battle cry"] = 85,
        ["Fate"] = 85,
        ["Expeditioner"] = 75,
        ["Trainer Skills"] = 70,
        ["Coworkers"] = 65,
        ["Meteorite Hunter"] = 60,
        ["Raining Gold"] = 55,
        ["All main attributes"] = 50,
        ["Auto attacks"] = 45, // alias: the user's "Party - Auto Abilities"
        ["Leader - Auto Abilities"] = 45,
        ["Damage Specialization"] = 40, // alias: the user's "Damage"
        ["Energy Heroes"] = 40,
        ["Mana Heroes"] = 40,
        ["Rage Heroes"] = 40,
        ["Fist Fight"] = 35,
        ["Precision"] = 35,
        ["Magic Spells"] = 35,
        ["Damage"] = 30,
        ["Healer Specialization"] = 30, // alias: the user's "Health"
        ["Armor"] = 25,
        ["Leadership"] = 25,
        ["Team Bonus"] = 25,
        ["Guardian Power"] = 25,
        ["Tank Specialization"] = 25, // alias: the user's "Armor"
        ["Critical damage"] = 20,
        ["Critical chance"] = 20,
        ["Attack speed"] = 20,
        ["Dodge"] = 15,
        ["Weaklings"] = 10,
        ["Expose Weakness"] = 10,
        ["Powerless enemy"] = 10,
        ["Powerless boss"] = 10,
        ["Ancient Knowledge"] = 0
    };

    /// <summary>Parses "Name:Priority,Name:Priority" (same idiom as ExperimentsTask.resource_type's
    ///     CSV parsing) and merges it over DefaultPriorities - an override wins outright, everything
    ///     else keeps its default. Malformed entries and names that don't match a real catalog node are
    ///     logged once and otherwise ignored, never thrown.</summary>
    public static IReadOnlyDictionary<string, int> Resolve(string overridesCsv)
    {
        var priorities = new Dictionary<string, int>(DefaultPriorities);
        if (string.IsNullOrWhiteSpace(overridesCsv)) return priorities;

        foreach (var entry in overridesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(':', 2);
            if (parts.Length != 2 || !int.TryParse(parts[1].Trim(), out var priority))
            {
                Logger.Debug($"[TalentBuildConfig] Ignoring malformed priority_overrides entry: '{entry}'.");
                continue;
            }

            priorities[parts[0].Trim()] = priority;
        }

        return priorities;
    }
}
