using System;
using System.Collections;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     Bounded waits for the game to catch up. Animations and screen transitions often outlast the
///     standard interaction delay, and their length isn't fixed, so the bot polls for the next state
///     instead of guessing a duration.
/// </summary>
public static class Poll
{
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
