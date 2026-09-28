using System;
using System.Collections;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     The game's quantity multipliers ("x1 / x10 / ...") are one button that cycles through its
///     options, with the current value in a label next to it. The options differ per screen and
///     aren't all known, so this searches instead of assuming.
/// </summary>
public static class QuantityToggle
{
    // More than any toggle's option count, so a full cycle always fits.
    private const int MaxClicks = 6;

    /// <summary>
    ///     x10 or x5, whichever the toggle reaches first: cost is proportional, so a bigger multiplier
    ///     only saves clicks.
    /// </summary>
    public static bool IsBulk(string text) => text.Contains("10") || text.Contains("5");

    /// <summary>
    ///     Clicks the toggle until the label satisfies isWanted. If the cycle wraps back to the
    ///     starting value (or MaxClicks pass) without a match, it keeps clicking until the original
    ///     value is back, so the caller is never left on some other multiplier by mistake.
    /// </summary>
    public static IEnumerator CycleUntil(GameButton toggle, GameText label, Func<string, bool> isWanted)
    {
        var original = label.GetParsedText();
        if (isWanted(original)) yield break;

        for (var i = 0; i < MaxClicks; i++)
        {
            yield return toggle.Click();

            var current = label.GetParsedText();
            if (isWanted(current)) yield break;
            if (current == original) break; // wrapped around - the wanted value isn't an option here
        }

        for (var i = 0; i < MaxClicks && label.GetParsedText() != original; i++)
            yield return toggle.Click();

        Logger.Debug($"[QuantityToggle] No wanted value at {label.FullPath}; left at '{label.GetParsedText()}'.");
    }
}
