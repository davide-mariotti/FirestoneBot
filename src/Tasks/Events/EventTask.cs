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

    // Each round claims what the last one finished and does the next tier: a Decorated Heroes card
    // has 3, so 4 rounds see the last claim.
    private const int MaxRounds = 4;

    // A round is ~15 s of event screens plus the actions (a crystal hit ~2 s, a tavern draw ~4 s).
    internal override float? MaxRuntimeSeconds => 600;

    public override IEnumerator Execute()
    {
        // The Done each kind must show after this run's own steps (progress counts on across tiers).
        // A card behind it is a read taken before the game caught up - acting on it would do the
        // same steps twice.
        var expected = new Dictionary<ChallengeKind, int>();

        for (var round = 1; round <= MaxRounds; round++)
        {
            string title = null;
            var challenges = new List<(string Text, string Progress)>();
            yield return Visit(challenges, t => title = t);
            if (title == null) yield break;

            // Screens closed: every action navigates on its own.
            var steps = 0;
            foreach (var (text, progress) in challenges)
            {
                var challenge = ChallengeParser.Parse(text, progress);
                if (challenge.Kind == ChallengeKind.NotHandled)
                {
                    Debug($"[INFO] Event '{title}': challenge not handled: '{text}' {progress}.");
                    continue;
                }

                Debug($"[INFO] Event '{title}': '{text}' {progress} -> {challenge.Kind}, {challenge.Missing} missing.");
                if (challenge.Missing == 0) continue;

                if (expected.TryGetValue(challenge.Kind, out var min) && challenge.Done < min)
                {
                    Debug($"[INFO] Event '{title}': {challenge.Kind} shows {challenge.Done}, {min} expected - left for the next run.");
                    continue;
                }

                var done = 0;
                var action = EventChallengeActions.Complete(challenge, n => done = n);
                if (action == null) continue;

                yield return action;
                expected[challenge.Kind] = challenge.Done + done;
                steps += done;
            }

            if (steps == 0) yield break;
            Debug($"[INFO] Event '{title}': {steps} step(s) done in round {round} - reopening to claim.");
        }
    }

    /// <summary>Opens the event, runs it and closes it. onOpened gets the card's title, or null when it didn't open.</summary>
    private IEnumerator Visit(List<(string Text, string Progress)> challenges, Action<string> onOpened)
    {
        yield return EventManager.Open;

        string title = null;
        yield return EventManager.OpenEvent(EventNames, t => title = t);
        yield return Poll.Until(() => IsScreenVisible);

        if (IsScreenVisible)
        {
            yield return RunEvent(challenges);
            NextRunTime = DateTime.Now + RecheckDelay;
            onOpened(title ?? "?");
        }
        else
        {
            if (title == null)
            {
                Debug("[INFO] Not open to this account (not listed, locked or upcoming) - backing off.");
                NextRunTime = DateTime.Now + RecheckDelay;
            }
            else
                Debug($"[FAILED] '{title}' didn't open - see the EventManager lines above.");

            onOpened(null);
        }

        yield return EventManager.Close;
    }
}
