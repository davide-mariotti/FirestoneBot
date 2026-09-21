using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Character;

/// <summary>
///     Static catalog of the 89 talent-tree nodes (Paths.TalentsLoc.NodeRoot, index 0-88) in tier
///     order, and the priority sequence to invest points in - transcribed from the user's
///     docs/talents-guide.html (itself transcribed from a Steam community guide; that file's own note
///     says its numbers are "as reported, not recalculated").
/// </summary>
public static class Talents
{
    // (name, max rank), one entry per talentInteraction (N) - N is the array index below. Tier order
    // confirmed via UnityPy: summing each tier's node count from the wiki's 46-tier table gives
    // exactly 89, matching talentInteraction (0)-(88) found live 1:1.
    private static readonly (string Name, int MaxRank)[] Catalog =
    {
        // Tier 1 (0 pts)
        ("All main attributes", 25),
        // Tier 2 (3 pts)
        ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25),
        // Tier 3 (10 pts)
        ("Heroes Auto Abilities", 1),
        // Tier 4 (15 pts)
        ("Attack speed", 25), ("Trainer Skills", 25), ("Critical chance", 25),
        // Tier 5 (20 pts)
        ("Guardian Auto Attack", 1),
        // Tier 6 (25 pts)
        ("Dodge", 25), ("Critical damage", 25),
        // Tier 7 (30 pts)
        ("Librarian", 25), ("Meteorite Hunter", 20), ("Expeditioner", 20),
        // Tier 8 (35 pts)
        ("Powerless enemy", 25), ("Powerless boss", 25),
        // Tier 9 (40 pts)
        ("Weaklings", 25), ("Expose weakness", 25),
        // Tier 10 (45 pts)
        ("Ancient Knowledge", 20),
        // Tier 11 (60 pts)
        ("Raining Gold", 5), ("Coworkers", 5),
        // Tier 12 (70 pts)
        ("Twin dragons", 10),
        // Tier 13 (80 pts)
        ("Attack speed", 25), ("Critical chance", 25),
        // Tier 14 (100 pts)
        ("Battle cry", 15),
        // Tier 15 (120 pts)
        ("Dodge", 25), ("Critical damage", 25),
        // Tier 16 (140 pts)
        ("Powerless enemy", 25), ("Powerless boss", 25),
        // Tier 17 (160 pts)
        ("Alchemy", 25),
        // Tier 18 (180 pts)
        ("Weaklings", 25), ("Expose weakness", 25),
        // Tier 19 (200 pts)
        ("All main attributes", 25),
        // Tier 20 (250 pts)
        ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25),
        // Tier 21 (300 pts)
        ("Twin dragons", 15),
        // Tier 22 (350 pts)
        ("Alchemy", 25), ("Librarian", 25),
        // Tier 23 (400 pts)
        ("Battle cry", 15),
        // Tier 24 (450 pts)
        ("Powerless enemy", 25), ("Powerless boss", 25),
        // Tier 25 (500 pts)
        ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25),
        // Tier 26 (530 pts)
        ("Fate", 15),
        // Tier 27 (560 pts)
        ("Mana Heroes", 25), ("Energy Heroes", 25), ("Rage Heroes", 25),
        // Tier 28 (590 pts)
        ("Weaklings", 25), ("Expose weakness", 25),
        // Tier 29 (620 pts)
        ("Damage Specialization", 25), ("Tank Specialization", 25), ("Healer Specialization", 25),
        // Tier 30 (650 pts)
        ("Raining Gold", 25),
        // Tier 31 (680 pts)
        ("Fist Fight", 25), ("Precision", 25), ("Magic Spells", 25),
        // Tier 32 (710 pts)
        ("Weaklings", 25), ("Expose weakness", 25),
        // Tier 33 (740 pts)
        ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25),
        // Tier 34 (770 pts)
        ("Powerless enemy", 25), ("Powerless boss", 25),
        // Tier 35 (800 pts)
        ("Fate", 15),
        // Tier 36 (830 pts)
        ("Mana Heroes", 25), ("Energy Heroes", 25), ("Rage Heroes", 25),
        // Tier 37 (860 pts)
        ("All main attributes", 25),
        // Tier 38 (890 pts)
        ("Fist Fight", 25), ("Precision", 25), ("Magic Spells", 25),
        // Tier 39 (920 pts)
        ("Weaklings", 25), ("Expose weakness", 25),
        // Tier 40 (950 pts)
        ("Battle cry", 15),
        // Tier 41 (980 pts)
        ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25),
        // Tier 42 (1010 pts)
        ("Powerless enemy", 25), ("Powerless boss", 25),
        // Tier 43 (1040 pts)
        ("Damage Specialization", 25), ("Tank Specialization", 25), ("Healer Specialization", 25),
        // Tier 44 (1070 pts)
        ("Raining Gold", 25),
        // Tier 45 (1100 pts)
        ("Mana Heroes", 25), ("Energy Heroes", 25), ("Rage Heroes", 25),
        // Tier 46 (1130 pts)
        ("Fate", 15)
    };

    // Priority order transcribed from docs/talents-guide.html, in sequence. Covers only the first
    // ~448 of the tree's 2037 total points (roughly up to Tier 24) - per the user, once every entry
    // here is satisfied the task stops and leaves further points unspent rather than guessing (a
    // reset costs 100 real gems, see TalentsLoc.ResetTreeBtnDoNotUse).
    //
    // Two entries renamed from the source guide's wording to match the catalog's wiki-confirmed
    // names - inferred by position (the only two 1-point/max-rank "auto" talents in the whole tree,
    // in the same relative order the guide lists them), then confirmed correct by the user: "Leader -
    // Auto Abilities" -> "Heroes Auto Abilities" (tier 3) and "Party - Auto Abilities" -> "Guardian
    // Auto Attack" (tier 5).
    //
    // The "free choice" entry (levels 341-350, "Critical damage / Critical chance", 11 pts) is
    // resolved to Critical damage - an arbitrary pick, the guide itself leaves it open.
    private static readonly (string Name, int Points)[] Guide =
    {
        ("All main attributes", 9),
        ("Leadership", 1),
        ("Heroes Auto Abilities", 1),
        ("All main attributes", 13),
        ("Trainer Skills", 5),
        ("Guardian Auto Attack", 1),
        ("Trainer Skills", 13),
        ("Critical damage", 1),
        ("Meteorite Hunter", 20),
        ("Trainer Skills", 19),
        ("Powerless boss", 1),
        ("Expose weakness", 1),
        ("Ancient Knowledge", 1),
        ("Raining Gold", 5),
        ("Coworkers", 5),
        ("Trainer Skills", 25),
        ("Librarian", 25),
        ("Expeditioner", 6),
        ("Twin dragons", 1),
        ("Critical chance", 1),
        ("Battle cry", 15),
        ("Expeditioner", 20),
        ("Twin dragons", 10),
        ("Ancient Knowledge", 20),
        ("Critical damage", 1),
        ("Powerless boss", 1),
        ("Alchemy", 25),
        ("Expose weakness", 1),
        ("All main attributes", 25),
        ("Leadership", 25),
        ("All main attributes", 25),
        ("Leadership", 25),
        ("Team Bonus", 25),
        ("Twin dragons", 15),
        ("Team Bonus", 25),
        ("Critical damage", 11), // free choice, see comment above
        ("Librarian", 25),
        ("Alchemy", 25),
        ("Battle cry", 15)
    };

    // Precomputed once: for each Guide entry, which Catalog index it resolves to and the rank that
    // node should reach once this entry is fully applied (capped at MaxRank - handles the source
    // guide's imprecise point counts, e.g. "Trainer Skills" is listed 4 times for a combined 62
    // points despite the node's real cap being 25 - the 3rd visit already reaches the cap, so the 4th
    // resolves to the same already-maxed target and is a no-op). A repeated name advances to the
    // tree's next instance of it only once the current one is simulated-maxed.
    public static readonly (int CatalogIndex, int TargetRank)[] Plan = BuildPlan();

    /// <summary>Display name for a catalog index, for calibration/diagnostic logging only - see
    /// TalentsTask.CalibrateGuideStartIndex.</summary>
    public static string CatalogName(int catalogIndex) => Catalog[catalogIndex].Name;

    private static (int, int)[] BuildPlan()
    {
        var instancesByName = new Dictionary<string, List<int>>();
        for (var i = 0; i < Catalog.Length; i++)
        {
            if (!instancesByName.TryGetValue(Catalog[i].Name, out var list))
                instancesByName[Catalog[i].Name] = list = new List<int>();
            list.Add(i);
        }

        var simulatedRank = new int[Catalog.Length];
        var pointer = new Dictionary<string, int>();
        var plan = new (int, int)[Guide.Length];

        for (var i = 0; i < Guide.Length; i++)
        {
            var (name, points) = Guide[i];
            var instances = instancesByName[name];
            pointer.TryGetValue(name, out var pos);

            while (pos < instances.Count - 1 && simulatedRank[instances[pos]] >= Catalog[instances[pos]].MaxRank)
                pos++;
            pointer[name] = pos;

            var idx = instances[pos];
            simulatedRank[idx] = Math.Min(Catalog[idx].MaxRank, simulatedRank[idx] + points);
            plan[i] = (idx, simulatedRank[idx]);
        }

        Validate(plan);
        return plan;
    }

    /// <summary>Fails loudly at class load rather than silently investing against a broken plan -
    /// this simulation is hand-transcribed data, not something covered by the project's usual
    /// live-verification-on-first-run flagging.</summary>
    private static void Validate((int CatalogIndex, int TargetRank)[] plan)
    {
        if (Catalog.Length != 89)
            throw new InvalidOperationException($"Talents.Catalog should have 89 nodes, has {Catalog.Length}.");

        foreach (var (idx, targetRank) in plan)
            if (targetRank < 0 || targetRank > Catalog[idx].MaxRank)
                throw new InvalidOperationException(
                    $"Talents.Plan target rank {targetRank} out of range for catalog index {idx} (max {Catalog[idx].MaxRank}).");
    }

    /// <summary>
    ///     Live-confirmed, 2026-09-18: the text reads "available/total" (e.g. "1/96", total points
    ///     ever earned - not the tree's max) - GetParsedInt()'s strict full-string parse silently
    ///     failed on this and fell back to 0, making the task think there was never anything to
    ///     spend. Takes the leading number, same fix already used for PreviewCurrentRank below.
    /// </summary>
    public static int AvailablePoints => new GameText(Paths.TalentsLoc.PointsLeftTxt).GetParsedLeadingInt();

    // Defensive - clicked once after any batch of upgrades in case investments are staged rather
    // than instant (see TalentsLoc.SaveBtn comment). Safe no-op if not needed/not clickable.
    public static IEnumerator Save => new GameButton(Paths.TalentsLoc.SaveBtn).Click();

    public static IEnumerator OpenNode(int catalogIndex) =>
        new GameButton($"{Paths.TalentsLoc.NodeRoot}/talentInteraction ({catalogIndex})").Click();

    public static IEnumerator ClosePreview => new GameButton(Paths.TalentPreviewLoc.CloseBtn).Click();

    public static bool IsPreviewLocked => new GameElement(Paths.TalentPreviewLoc.LockedRoot).IsVisible();

    public static GameButton UpgradeButton => new(Paths.TalentPreviewLoc.UpgradeBtn);

    /// <summary>
    ///     Current rank shown on the open TalentPreview popup. Exact text format ("5" vs "5/25") not
    ///     verified live - takes the leading number either way.
    /// </summary>
    public static int PreviewCurrentRank => new GameText(Paths.TalentPreviewLoc.LevelTxt).GetParsedLeadingInt();
}
