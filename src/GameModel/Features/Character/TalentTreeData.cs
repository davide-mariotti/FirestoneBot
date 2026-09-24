using System.Collections.Generic;
using Firebot.TalentEngine;

namespace Firebot.GameModel.Features.Character;

/// <summary>
///     Firestone's real 89-node, 46-tier talent tree, as a <see cref="TalentTreeDefinition" /> for
///     <see cref="TalentEngine.TalentAllocator" />. Names and grouping live-verified node-by-node on
///     Steam-0, 2026-09-24 (opened every talentInteraction (0)-(88) and read the preview's real
///     "talentName" text) - this replaced an earlier wiki-transcribed catalog that had two nodes
///     wrong: index 4 is really "Leader - Auto Abilities" (not "Heroes Auto Abilities") and index 8 is
///     really "Auto attacks" (not "Guardian Auto Attack") - both were guesses inferring which of the
///     source guide's two "Auto Abilities" entries matched which catalog slot, and guessed wrong on the
///     second one. Tier cumulative point thresholds (the wiki's per-tier "spend N total points to
///     unlock this tier") are unchanged from the wiki and were never in question.
///     No node has a PrerequisiteIndex set - see TalentsTask's own doc comment for why the tree's real
///     per-node "direct predecessor" prerequisites are deliberately left unmapped and handled reactively
///     instead (Paths.TalentPreviewLoc.LockedRoot, checked live at investment time).
/// </summary>
public static class TalentTreeData
{
    // (Name, MaxRank) per node, grouped by tier in the exact order the wiki's 46-tier table (and the
    // live grid) lists them - same shape as the old Catalog, only the two names above corrected and
    // capitalization normalized to match what the game actually displays.
    private static readonly (string Name, int MaxRank)[][] Tiers =
    {
        new[] { ("All main attributes", 25) }, // Tier 1 (0 pts)
        new[] { ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25) }, // Tier 2 (3 pts)
        new[] { ("Leader - Auto Abilities", 1) }, // Tier 3 (10 pts)
        new[] { ("Attack speed", 25), ("Trainer Skills", 25), ("Critical chance", 25) }, // Tier 4 (15 pts)
        new[] { ("Auto attacks", 1) }, // Tier 5 (20 pts)
        new[] { ("Dodge", 25), ("Critical damage", 25) }, // Tier 6 (25 pts)
        new[] { ("Librarian", 25), ("Meteorite Hunter", 20), ("Expeditioner", 20) }, // Tier 7 (30 pts)
        new[] { ("Powerless enemy", 25), ("Powerless boss", 25) }, // Tier 8 (35 pts)
        new[] { ("Weaklings", 25), ("Expose Weakness", 25) }, // Tier 9 (40 pts)
        new[] { ("Ancient Knowledge", 20) }, // Tier 10 (45 pts)
        new[] { ("Raining Gold", 5), ("Coworkers", 5) }, // Tier 11 (60 pts)
        new[] { ("Twin dragons", 10) }, // Tier 12 (70 pts)
        new[] { ("Attack speed", 25), ("Critical chance", 25) }, // Tier 13 (80 pts)
        new[] { ("Battle cry", 15) }, // Tier 14 (100 pts)
        new[] { ("Dodge", 25), ("Critical damage", 25) }, // Tier 15 (120 pts)
        new[] { ("Powerless enemy", 25), ("Powerless boss", 25) }, // Tier 16 (140 pts)
        new[] { ("Alchemy", 25) }, // Tier 17 (160 pts)
        new[] { ("Weaklings", 25), ("Expose Weakness", 25) }, // Tier 18 (180 pts)
        new[] { ("All main attributes", 25) }, // Tier 19 (200 pts)
        new[] { ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25) }, // Tier 20 (250 pts)
        new[] { ("Twin dragons", 15) }, // Tier 21 (300 pts)
        new[] { ("Alchemy", 25), ("Librarian", 25) }, // Tier 22 (350 pts)
        new[] { ("Battle cry", 15) }, // Tier 23 (400 pts)
        new[] { ("Powerless enemy", 25), ("Powerless boss", 25) }, // Tier 24 (450 pts)
        new[] { ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25) }, // Tier 25 (500 pts)
        new[] { ("Fate", 15) }, // Tier 26 (530 pts)
        new[] { ("Mana Heroes", 25), ("Energy Heroes", 25), ("Rage Heroes", 25) }, // Tier 27 (560 pts)
        new[] { ("Weaklings", 25), ("Expose Weakness", 25) }, // Tier 28 (590 pts)
        new[] { ("Damage Specialization", 25), ("Tank Specialization", 25), ("Healer Specialization", 25) }, // Tier 29 (620 pts)
        new[] { ("Raining Gold", 25) }, // Tier 30 (650 pts)
        new[] { ("Fist Fight", 25), ("Precision", 25), ("Magic Spells", 25) }, // Tier 31 (680 pts)
        new[] { ("Weaklings", 25), ("Expose Weakness", 25) }, // Tier 32 (710 pts)
        new[] { ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25) }, // Tier 33 (740 pts)
        new[] { ("Powerless enemy", 25), ("Powerless boss", 25) }, // Tier 34 (770 pts)
        new[] { ("Fate", 15) }, // Tier 35 (800 pts)
        new[] { ("Mana Heroes", 25), ("Energy Heroes", 25), ("Rage Heroes", 25) }, // Tier 36 (830 pts)
        new[] { ("All main attributes", 25) }, // Tier 37 (860 pts)
        new[] { ("Fist Fight", 25), ("Precision", 25), ("Magic Spells", 25) }, // Tier 38 (890 pts)
        new[] { ("Weaklings", 25), ("Expose Weakness", 25) }, // Tier 39 (920 pts)
        new[] { ("Battle cry", 15) }, // Tier 40 (950 pts)
        new[] { ("Leadership", 25), ("Guardian Power", 25), ("Team Bonus", 25) }, // Tier 41 (980 pts)
        new[] { ("Powerless enemy", 25), ("Powerless boss", 25) }, // Tier 42 (1010 pts)
        new[] { ("Damage Specialization", 25), ("Tank Specialization", 25), ("Healer Specialization", 25) }, // Tier 43 (1040 pts)
        new[] { ("Raining Gold", 25) }, // Tier 44 (1070 pts)
        new[] { ("Mana Heroes", 25), ("Energy Heroes", 25), ("Rage Heroes", 25) }, // Tier 45 (1100 pts)
        new[] { ("Fate", 15) } // Tier 46 (1130 pts)
    };

    private static readonly int[] Thresholds =
    {
        0, 3, 10, 15, 20, 25, 30, 35, 40, 45, 60, 70, 80, 100, 120, 140, 160, 180, 200, 250, 300, 350,
        400, 450, 500, 530, 560, 590, 620, 650, 680, 710, 740, 770, 800, 830, 860, 890, 920, 950, 980,
        1010, 1040, 1070, 1100, 1130
    };

    public static readonly TalentTreeDefinition Tree = Build();

    private static TalentTreeDefinition Build()
    {
        var nodes = new List<TalentNode>();

        for (var tier = 0; tier < Tiers.Length; tier++)
            foreach (var (name, maxRank) in Tiers[tier])
                nodes.Add(new TalentNode(nodes.Count, name, maxRank, tier));

        return new TalentTreeDefinition(nodes, Thresholds);
    }

    public static string NodeName(int index) => Tree.Nodes[index].Name;
}
