using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Features.Events;
using Firebot.GameModel.Shared;

namespace Firebot.Tasks.Events;

/// <summary>
///     Every mini-event: one runs every 5 days for 3 days, each under its own card, and all of them
///     open the game's MiniEvents screen. Claims every unlocked day on the challenges tab. The
///     default "offers" tab is paid and never touched.
/// </summary>
public class MiniEventTask : EventTask
{
    // The wiki's 11 mini-events. Seen live: "Mass Production", "Sigils of prophecy" (opens
    // MiniEvents), "Stardust" and "Primordial elements" (upcoming cards, 30/09).
    protected override string[] EventNames => new[]
    {
        "Mass Production", "Sigils of Prophecy", "Stardust", "Primordial Elements", "Ethereal Miners",
        "Team Effort", "Mechanical Superiority", "World Domination", "Guardians of Destiny",
        "Blessing of the Eternals", "Champions of Alandria"
    };

    protected override bool IsScreenVisible => MiniEvents.IsVisible;

    protected override bool? RunningFromGame => GameState.MiniEventRunning;

    protected override string Describe => GameState.MiniEventDescribe;

    /// <summary>The game's kill-challenge hero a card named at a visit this session (FormationTask); null before.</summary>
    public static string KillHeroConfirmed { get; private set; }

    protected override List<(ChallengeKind Kind, int Done, int Target, bool Claimable)> ChallengesFromGame => GameState.MiniChallenges;

    protected override IEnumerator RunEvent(List<(string Text, string Progress)> challenges)
    {
        Debug($"[INFO] From the game at this visit: running {RunningFromGame}, {Describe}.");
        yield return MiniEvents.OpenChallengesTab;
        yield return MiniEvents.ClaimAllChallenges();
        challenges.AddRange(MiniEvents.Challenges());

        // The game's "kill with a hero" challenge against the cards: FormationTask follows it only once
        // a card names the same hero (a hero-less kill challenge's code could read as the first hero).
        if (GameState.MiniEventKillHero is { } hero)
        {
            var named = challenges.Any(c => c.Text.IndexOf(hero, System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (named) KillHeroConfirmed = hero;
            Debug($"[INFO] Mini-event kill challenge for {hero}: {(named ? "a card names it" : "no card names it - not followed")}.");
        }

        yield return MiniEvents.Close;
    }
}
