using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Events;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Events;

/// <summary>
///     An event opened from Battle -> Events, on the Events button's badge and hourly otherwise, so
///     new challenges and check-ins don't pile up. The badge is shared by every event, so all of them
///     answer it. An event missing from the account's list backs off the same way; a card that
///     didn't open its screen leaves NextRunTime alone, so the scheduler's short idle retry applies.
/// </summary>
public abstract class EventTask : BotTask
{
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(1);

    internal override TaskGroup Group => TaskGroup.Events;

    // The Desktop HUD variant's eventsButton has no children.
    protected override string[] NotificationPaths => new[]
    {
        Paths.BattleLoc.RightSideUILoc.EventsNotification,
        Paths.BattleLoc.BottomSideUIMobileLoc.EventsNotification
    };

    /// <summary>The event card's title in the Events list.</summary>
    protected abstract string EventName { get; }

    protected abstract bool IsScreenVisible { get; }

    /// <summary>The event's own work, ending with its screen closed.</summary>
    protected abstract IEnumerator RunEvent();

    public override IEnumerator Execute()
    {
        yield return EventManager.Open;

        var cardFound = false;
        yield return EventManager.OpenEvent(EventName, found => cardFound = found);
        yield return Poll.Until(() => IsScreenVisible);

        if (IsScreenVisible)
        {
            yield return RunEvent();
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else if (!cardFound)
        {
            Debug($"[INFO] '{EventName}' isn't in this account's event list - backing off.");
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else
            Debug($"[FAILED] '{EventName}' didn't open - see the EventManager lines above.");

        yield return EventManager.Close;
    }
}
