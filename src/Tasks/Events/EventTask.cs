using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Events;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Events;

/// <summary>
///     An event opened from Battle -> Events, on the Events button's badge and hourly otherwise, so
///     new challenges and check-ins don't pile up. The badge is shared by every event, so all of them
///     answer it. An event missing from the account's list, or locked for it, backs off the same way;
///     a card that didn't open its screen leaves NextRunTime alone, so the scheduler's short idle
///     retry applies.
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

    /// <summary>The event card's titles in the Events list; the first unlocked match is opened.</summary>
    protected abstract string[] EventNames { get; }

    protected abstract bool IsScreenVisible { get; }

    /// <summary>
    ///     The event's own work, ending with its screen closed. Adds each challenge card's text and
    ///     progress, read after the claims, to challenges.
    /// </summary>
    protected abstract IEnumerator RunEvent(List<(string Text, string Progress)> challenges);

    public override IEnumerator Execute()
    {
        yield return EventManager.Open;

        string title = null;
        yield return EventManager.OpenEvent(EventNames, t => title = t);
        yield return Poll.Until(() => IsScreenVisible);

        if (IsScreenVisible)
        {
            var challenges = new List<(string Text, string Progress)>();
            yield return RunEvent(challenges);
            NextRunTime = DateTime.Now + RecheckDelay;

            foreach (var (text, progress) in challenges)
            {
                var challenge = ChallengeParser.Parse(text, progress);
                Debug(challenge.Kind == ChallengeKind.NotHandled
                    ? $"[INFO] Event '{title}': challenge not handled: '{text}' {progress}."
                    : $"[INFO] Event '{title}': '{text}' {progress} -> {challenge.Kind}, {challenge.Missing} missing.");
            }
        }
        else if (title == null)
        {
            Debug("[INFO] Not open to this account (not listed, locked or upcoming) - backing off.");
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else
            Debug($"[FAILED] '{title}' didn't open - see the EventManager lines above.");

        yield return EventManager.Close;
    }
}
