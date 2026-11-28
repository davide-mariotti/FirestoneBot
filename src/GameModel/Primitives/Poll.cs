using System;
using System.Collections;
using Firebot.GameModel.Shared;
using UnityEngine;
using static Firebot.Core.BotSettings;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     Bounded waits for the game to catch up. Animations and screen transitions often outlast the
///     standard interaction delay, and their length isn't fixed, so the bot polls for the next state
///     instead of guessing a duration.
/// </summary>
public static class Poll
{
    /// <summary>
    ///     The wait after a click that happened: until the game is done with it (its activity blocker off
    ///     and the menu stack unchanged for 0.1 s), at least half the interaction delay and at most twice
    ///     it. The 26/11 probe: a menu opening keeps the blocker on for 0.7-1.2 s, a click inside a
    ///     screen settles in 0.1-0.2 s. Without the game's state, the full delay as before.
    /// </summary>
    public static IEnumerator AfterClick(string path)
    {
        var start = Time.realtimeSinceStartup;
        float min = InteractionDelay / 2, max = InteractionDelay * 2;
        var lastStack = GameState.MenuStack;
        var stableSince = start;
        var step = new WaitForSeconds(0.05f);

        while (true)
        {
            yield return step;
            var now = Time.realtimeSinceStartup;
            var busy = GameState.Busy;
            var stack = GameState.MenuStack;
            if (busy == null || stack == null)
            {
                if (now - start >= InteractionDelay) yield break;
                continue;
            }

            if (stack != lastStack || busy.Value)
            {
                lastStack = stack;
                stableSince = now;
            }

            if (now - start >= min && now - stableSince >= 0.1f) yield break;
            if (now - start < max) continue;

            Logger.Debug($"[Poll] {path}: the game was still busy after {max:0.#}s ({stack}).");
            yield break;
        }
    }

    /// <summary>Waits until done() is true, at most maxPolls x pollSeconds. Callers re-check done() after.</summary>
    public static IEnumerator Until(Func<bool> done, int maxPolls = 10, float pollSeconds = 0.5f)
    {
        var wait = new WaitForSeconds(pollSeconds);
        for (var i = 0; i < maxPolls && !done(); i++)
            yield return wait;
    }

    /// <summary>
    ///     Simulated-clicks a button and waits for done(), retrying the click itself up to maxAttempts
    ///     times - some buttons intermittently ignore a click entirely rather than just responding late.
    ///     The button is resolved again on every attempt, for HUD buttons that move between variants.
    /// </summary>
    public static IEnumerator ClickUntil(Func<GameButton> button, Func<bool> done, string label)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts && !done(); attempt++)
        {
            var target = button();
            yield return target.ClickSimulated();
            yield return Until(done);

            Logger.Debug($"[{label}] attempt {attempt}/{maxAttempts} on '{target.FullPath}': done={done()}.");
        }
    }
}
