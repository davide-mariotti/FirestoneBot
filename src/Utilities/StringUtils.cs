using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Firebot.Utilities;

public static class StringUtils
{
    /// <summary>"GuardianTrainingTask" -> "Guardian Training": strips the suffix, spaces out PascalCase.</summary>
    public static string Humanize(string input, string suffixToRemove = "Task")
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var cleaned = input.EndsWith(suffixToRemove)
            ? input.Substring(0, input.Length - suffixToRemove.Length)
            : input;

        return Regex.Replace(cleaned, @"(?<!^)(?=[A-Z])", " ").Trim();
    }

    /// <summary>All digits in the string read as one integer: "x5" -> 5, "Level 12" -> 12.</summary>
    public static bool TryParseIntFromString(string input, out int value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;
        var digits = new string(input.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out value);
    }

    // A plain '.'-grouped integer ("17.242", "1.234.567"): every group after the first has exactly 3
    // digits, which a real decimal fraction in this game's UI never does.
    private static readonly Regex DotGroupedInteger = new(@"^\d{1,3}(\.\d{3})+$", RegexOptions.Compiled);

    /// <summary>
    ///     Game-formatted numbers: abbreviated ("86,27M", "1,79bl"), '.'-grouped ("17.242") or European
    ///     ("10.916.942.093,4"), optionally behind a label ("Arena power: 18.336"). Past T the game
    ///     writes two lowercase letters, aa = 1e15 and x1000 per letter step (the wiki's Map table:
    ///     128T x20 = 2.56aa, 51.2aa x20 = 1.0ab).
    /// </summary>
    public static double ParseAbbreviated(string text, double fallback = 0)
    {
        text = text?.Trim() ?? "";
        if (text.Length == 0) return fallback;

        var colonIndex = text.LastIndexOf(':');
        if (colonIndex >= 0) text = text[(colonIndex + 1)..].Trim();
        if (text.Length == 0) return fallback;

        var multiplier = 1d;
        if (text.Length >= 3 && text[^1] is >= 'a' and <= 'z' && text[^2] is >= 'a' and <= 'z')
        {
            // ponytail: a double overflows past "dt" (1e306); switch the callers to log scale if an account gets there.
            multiplier = Math.Pow(10, 15 + 3 * ((text[^2] - 'a') * 26 + (text[^1] - 'a')));
            text = text[..^2].Trim();
        }
        else if (char.ToUpperInvariant(text[^1]) is 'K' or 'M' or 'B' or 'T')
        {
            multiplier = char.ToUpperInvariant(text[^1]) switch { 'K' => 1e3, 'M' => 1e6, 'B' => 1e9, _ => 1e12 };
            text = text[..^1].Trim();
        }

        // Before the invariant parse, which would happily read "17.242" as seventeen point something.
        if (DotGroupedInteger.IsMatch(text) &&
            long.TryParse(text.Replace(".", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var groupedValue))
            return groupedValue * multiplier;

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantValue))
            return invariantValue * multiplier;

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var currentCultureValue))
            return currentCultureValue * multiplier;

        var europeanStyle = text.Replace(".", "").Replace(",", ".");
        return double.TryParse(europeanStyle, NumberStyles.Float, CultureInfo.InvariantCulture, out var europeanValue)
            ? europeanValue * multiplier
            : fallback;
    }
}
