using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Features.Events;

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

    protected override IEnumerator RunEvent(List<(string Text, string Progress)> challenges)
    {
        yield return MiniEvents.OpenChallengesTab;
        yield return MiniEvents.ClaimAllChallenges();
        challenges.AddRange(MiniEvents.Challenges());

        yield return MiniEvents.Close;
    }
}
