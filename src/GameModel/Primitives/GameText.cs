using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Firebot.GameModel.Base;
using Firebot.Utilities;
using Il2CppTMPro;
using UnityEngine;

namespace Firebot.GameModel.Primitives;

public class GameText : GameElement
{
    public GameText(string path = null, GameElement parent = null, Transform transform = null)
        : base(path, parent, transform) { }

    /// <summary>Now + the countdown shown, or DateTime.MinValue when the text shows none.</summary>
    public DateTime Time => TimeParser.ParseExpectedTime(GetParsedText());

    /// <summary>The whole text as an int ("42"); fallback when it's anything else.</summary>
    public int GetParsedInt(int fallback = 0)
    {
        var parsedText = GetParsedText();
        return int.TryParse(parsedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    /// <summary>The number before the '/' of a "current/total" counter ("1/96" -> 1).</summary>
    public int GetParsedLeadingInt(int fallback = 0)
    {
        var text = GetParsedText();
        var slashIndex = text.IndexOf('/');
        var head = slashIndex >= 0 ? text[..slashIndex] : text;
        return int.TryParse(head.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private static readonly Regex FirstDigitRun = new(@"\d+", RegexOptions.Compiled);

    /// <summary>The first run of digits anywhere in the text ("Level 1/25" -> 1).</summary>
    public int GetParsedFirstInt(int fallback = 0)
    {
        var match = FirstDigitRun.Match(GetParsedText());
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : fallback;
    }

    /// <summary>The number after the '/' of a "current/total" counter ("1/96" -> 96).</summary>
    public int GetParsedTrailingInt(int fallback = 0)
    {
        var text = GetParsedText();
        var slashIndex = text.LastIndexOf('/');
        var tail = slashIndex >= 0 ? text[(slashIndex + 1)..] : text;
        return int.TryParse(tail.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    // A plain '.'-grouped integer ("17.242", "1.234.567"): every group after the first has exactly 3
    // digits, which a real decimal fraction in this game's UI never does.
    private static readonly Regex DotGroupedInteger = new(@"^\d{1,3}(\.\d{3})+$", RegexOptions.Compiled);

    /// <summary>
    ///     Game-formatted numbers: abbreviated ("86,27M"), '.'-grouped ("17.242") or European
    ///     ("10.916.942.093,4"), optionally behind a label ("Arena power: 18.336").
    /// </summary>
    public double GetParsedDoubleAbbreviated(double fallback = 0)
    {
        var text = GetParsedText().Trim();
        if (text.Length == 0) return fallback;

        var colonIndex = text.LastIndexOf(':');
        if (colonIndex >= 0) text = text[(colonIndex + 1)..].Trim();

        var multiplier = 1d;
        var suffix = char.ToUpperInvariant(text[^1]);
        if (suffix is 'K' or 'M' or 'B' or 'T')
        {
            multiplier = suffix switch { 'K' => 1e3, 'M' => 1e6, 'B' => 1e9, _ => 1e12 };
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

    public string GetParsedText()
    {
        if (!IsVisible()) return string.Empty;

        if (!TryGetComponent(out TMP_Text tmp)) return string.Empty;
        try
        {
            return tmp.text;
        }
        catch (Exception e)
        {
            Debug($"[FAILED] Exception while reading text: {e.Message}. Path: {Path}");
            return string.Empty;
        }
    }
}
