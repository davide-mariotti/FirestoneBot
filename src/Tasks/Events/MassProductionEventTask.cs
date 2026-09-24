using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Events;

namespace Firebot.Tasks.Events;

/// <summary>
///     Mass Production - the real in-game screen is a recycled "MiniEvents" prefab/script (see
///     MiniEvents' own doc comment). Battle -&gt; Events -&gt; "Mass Production" -&gt; Challenges tab
///     (claim every unlocked day) - per the user, 2026-09-24, the default "offers" tab (real-money
///     packs) is deliberately never touched, same convention as every other event shop's excluded
///     real-money tab.
/// </summary>
public class MassProductionEventTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Events;

    // Recurring event, no fixed daily reset like a quest - recheck a few times a day so a newly
    // unlocked day's challenge gets claimed promptly, same cadence reasoning as
    // DecoratedHeroesEventTask.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(4);

    public override IEnumerator Execute()
    {
        yield return EventManager.Open;
        Debug($"[INFO] EventManager hub visible after Open: {EventManager.IsVisible}");

        yield return EventManager.OpenEvent("Mass Production");
        yield return MiniEvents.WaitUntilOpen();
        Debug($"[INFO] MiniEvents visible after OpenEvent: {MiniEvents.IsVisible}");

        if (MiniEvents.IsVisible)
        {
            yield return MiniEvents.OpenChallengesTab;
            yield return MiniEvents.ClaimAllChallenges();

            yield return MiniEvents.Close;

            // Only schedule the long recheck on an actual completed pass - same reasoning as
            // DecoratedHeroesEventTask's own fix: if the shop never opened, leaving NextRunTime
            // untouched lets BotManager's 2-minute idle floor retry soon instead of going dark for
            // 4 hours on every failure.
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else
        {
            Debug("[INFO] MiniEvents never opened - see EventManager/OpenEvent debug lines above for why.");
        }

        yield return EventManager.Close;
    }
}
