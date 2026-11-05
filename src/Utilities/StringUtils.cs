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
    public static double ParseAbbreviated(string text, double fallback = 0) =>
        TryParseAbbreviated(text, out var number, out var exponent) ? number * Math.Pow(10, exponent) : fallback;

    /// <summary>
    ///     log10 of ParseAbbreviated's number, for what a double can't hold: past 1.8e308 ("dt" is 1e306)
    ///     it reads Infinity. -Infinity when the text is 0 or unreadable.
    /// </summary>
    public static double ParseAbbreviatedLog10(string text) =>
        TryParseAbbreviated(text, out var number, out var exponent) && number > 0
            ? Math.Log10(number) + exponent
            : double.NegativeInfinity;

    private static bool TryParseAbbreviated(string text, out double number, out int exponent)
    {
        number = 0;
        exponent = 0;

        text = text?.Trim() ?? "";
        if (text.Length == 0) return false;

        var colonIndex = text.LastIndexOf(':');
        if (colonIndex >= 0) text = text[(colonIndex + 1)..].Trim();
        if (text.Length == 0) return false;

        if (text.Length >= 3 && text[^1] is >= 'a' and <= 'z' && text[^2] is >= 'a' and <= 'z')
        {
            exponent = 15 + 3 * ((text[^2] - 'a') * 26 + (text[^1] - 'a'));
            text = text[..^2].Trim();
        }
        else if (char.ToUpperInvariant(text[^1]) is 'K' or 'M' or 'B' or 'T')
        {
            exponent = char.ToUpperInvariant(text[^1]) switch { 'K' => 3, 'M' => 6, 'B' => 9, _ => 12 };
            text = text[..^1].Trim();
        }

        // Before the invariant parse, which would happily read "17.242" as seventeen point something.
        if (DotGroupedInteger.IsMatch(text) &&
            long.TryParse(text.Replace(".", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var groupedValue))
        {
            number = groupedValue;
            return true;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) ||
               double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out number) ||
               double.TryParse(text.Replace(".", "").Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }
}
