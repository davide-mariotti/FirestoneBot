using System.Collections;
using Firebot.GameModel.Features.Events;

namespace Firebot.Tasks.Events;

/// <summary>
///     Mass Production (its screen is the game's MiniEvents): claims every unlocked day on the
///     challenges tab. The default "offers" tab is paid and never touched.
/// </summary>
public class MassProductionEventTask : EventTask
{
    protected override string EventName => "Mass Production";

    protected override bool IsScreenVisible => MiniEvents.IsVisible;

    protected override IEnumerator RunEvent()
    {
        yield return MiniEvents.OpenChallengesTab;
        yield return MiniEvents.ClaimAllChallenges();

        yield return MiniEvents.Close;
    }
}
