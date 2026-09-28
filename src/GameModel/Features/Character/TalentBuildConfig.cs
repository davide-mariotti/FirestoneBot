using System;
using System.Collections.Generic;
using Firebot.Core;

namespace Firebot.GameModel.Features.Character;

/// <summary>
///     Default investment priority per talent name (higher first). A name repeated across tiers
///     shares one priority - the allocator only needs the relative order of whatever is open.
///     Librarian (research speed) and Alchemy lead, matching the F2P guide's "time reductions first".
///     Overridable per account through the priority_overrides setting.
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
        ["Auto attacks"] = 45,
        ["Leader - Auto Abilities"] = 45,
        ["Damage Specialization"] = 40,
        ["Energy Heroes"] = 40,
        ["Mana Heroes"] = 40,
        ["Rage Heroes"] = 40,
        ["Fist Fight"] = 35,
        ["Precision"] = 35,
        ["Magic Spells"] = 35,
        ["Damage"] = 30,
        ["Healer Specialization"] = 30,
        ["Armor"] = 25,
        ["Leadership"] = 25,
        ["Team Bonus"] = 25,
        ["Guardian Power"] = 25,
        ["Tank Specialization"] = 25,
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

    /// <summary>
    ///     Merges "Name:Priority,Name:Priority" over the defaults. Malformed entries are logged and
    ///     skipped; a name that matches no talent simply has no effect.
    /// </summary>
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
