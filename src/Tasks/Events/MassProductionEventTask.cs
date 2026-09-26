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

    // Per the user (2026-09-26): recheck hourly instead of every 4h, so a newly unlocked day's
    // challenge gets claimed promptly, same cadence reasoning as DecoratedHeroesEventTask.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(1);

    public override IEnumerator Execute()
    {
        yield return EventManager.Open;
        Debug($"[INFO] EventManager hub visible after Open: {EventManager.IsVisible}");

        var cardFound = false;
        yield return EventManager.OpenEvent("Mass Production", found => cardFound = found);
        yield return MiniEvents.WaitUntilOpen();
        Debug($"[INFO] MiniEvents visible after OpenEvent: {MiniEvents.IsVisible}, cardFound: {cardFound}");

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
        else if (!cardFound)
        {
            // Live-confirmed, 2026-09-26 (Steam-0, New Player Event's identical situation): if the
            // event isn't even in the account's list, retrying every 2 minutes forever wastes a full
            // scan cycle for nothing - back off to the normal cadence instead.
            Debug("[INFO] 'Mass Production' isn't in this account's event list - backing off to the normal cadence.");
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else
        {
            Debug("[INFO] MiniEvents never opened - see EventManager/OpenEvent debug lines above for why.");
        }

        yield return EventManager.Close;
    }
}
