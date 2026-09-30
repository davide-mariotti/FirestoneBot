#nullable enable
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Firebot.Tasks.Events;

/// <summary>What an event challenge asks for, among those the bot can do on the spot.</summary>
public enum ChallengeKind
{
    NotHandled,
    CrystalHits,
    TavernPlays,
    OpenChests,
    SellItems,
    TreeOfLifeUpgrades,
    MeteoriteResearches
}

/// <summary>A challenge card read as (kind, done/target). Missing is 0 once the target is met, or unreadable.</summary>
public readonly record struct Challenge(ChallengeKind Kind, int Done, int Target)
{
    public int Missing => Math.Max(0, Target - Done);
}

/// <summary>
///     Reads a challenge card's text and progress. The rules below are the only place to update when
///     the game changes a text. Anything else (missions, researches, time online...) is NotHandled:
///     the tasks already running complete those on their own.
/// </summary>
public static class ChallengeParser
{
    // Live texts (Steam-0, 30/09): "Hit the arcane crystal 10 times.", "Play 12 times with the cards
    // at the tavern." (Decorated Heroes), "Complete 1 meteorite researches." (mini-event). The rest
    // are the wiki's mini-event texts, not seen live yet.
    private static readonly (Regex Text, ChallengeKind Kind)[] Rules =
    {
        (Rule(@"^Hit the arcane crystal \d+ times"), ChallengeKind.CrystalHits),
        (Rule(@"^Play \d+ times .*tavern"), ChallengeKind.TavernPlays),
        (Rule(@"^Open \d+ chests"), ChallengeKind.OpenChests),
        (Rule(@"^Sell \d+ items at the exotic merchant"), ChallengeKind.SellItems),
        (Rule(@"^Complete \d+ upgrades at your personal tree of life"), ChallengeKind.TreeOfLifeUpgrades),
        (Rule(@"^Complete \d+ meteorite researches"), ChallengeKind.MeteoriteResearches)
    };

    private static Regex Rule(string pattern) => new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // "5/10"; "Completed" after the last tier doesn't match.
    private static readonly Regex Progress = new(@"^\s*(\d+)\s*/\s*(\d+)\s*$");

    public static Challenge Parse(string text, string progress)
    {
        var kind = ChallengeKind.NotHandled;
        foreach (var (rule, ruleKind) in Rules)
            if (rule.IsMatch(text.Trim()))
            {
                kind = ruleKind;
                break;
            }

        var match = Progress.Match(progress);
        return match.Success
            ? new Challenge(kind, Int(match.Groups[1].Value), Int(match.Groups[2].Value))
            : new Challenge(kind, 0, 0);
    }

    private static int Int(string digits) => int.Parse(digits, CultureInfo.InvariantCulture);
}
