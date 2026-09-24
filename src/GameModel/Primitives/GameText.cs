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

    public DateTime Time => TimeParser.ParseExpectedTime(GetParsedText());

    public DateTime TimeMultiplier(double multiplier = 1) =>
        TimeParser.ParseExpectedTime(GetParsedText(), multiplier: multiplier);

    public int GetParsedInt(int fallback = 0)
    {
        var parsedText = GetParsedText();
        return int.TryParse(parsedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    /// <summary>
    ///     Same as GetParsedInt, but for "current/total" style counters (e.g. "1/96", "3/5") - the
    ///     strict full-string parse in GetParsedInt silently fails on these and always falls back,
    ///     first found live on Talents' available-points counter. Takes the number before the first
    ///     '/', or the whole (trimmed) text if there isn't one.
    /// </summary>
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

    /// <summary>
    ///     Same idea as GetParsedLeadingInt, but for text with a non-numeric label glued in front of the
    ///     number instead of (or in addition to) a "/total" suffix - e.g. TalentPreview's rank counter
    ///     reads "Level 1/25", not "1/25" (live-confirmed, 2026-09-24), which GetParsedLeadingInt can't
    ///     handle since "Level 1" doesn't parse as a plain int. Takes the first contiguous run of digits
    ///     found anywhere in the text, ignoring everything else.
    /// </summary>
    public int GetParsedFirstInt(int fallback = 0)
    {
        var match = FirstDigitRun.Match(GetParsedText());
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : fallback;
    }

    /// <summary>
    ///     Symmetric with GetParsedLeadingInt - for "current/total" style counters, takes the number
    ///     after the last '/' instead (the "total" side), or the whole (trimmed) text if there isn't
    ///     one. First needed by Talents.TotalPointsAwarded to get the talent tree's cumulative spent
    ///     total in one read instead of summing every node's live rank.
    /// </summary>
    public int GetParsedTrailingInt(int fallback = 0)
    {
        var text = GetParsedText();
        var slashIndex = text.LastIndexOf('/');
        var tail = slashIndex >= 0 ? text[(slashIndex + 1)..] : text;
        return int.TryParse(tail.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    public double GetParsedDouble(double fallback = 0)
    {
        var parsedText = GetParsedText();

        if (double.TryParse(parsedText, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantValue))
            return invariantValue;

        return double.TryParse(parsedText, NumberStyles.Float, CultureInfo.CurrentCulture, out var currentCultureValue)
            ? currentCultureValue
            : fallback;
    }

    // Matches a plain '.'-grouped integer with no decimal part at all (e.g. "17.242", "1.234.567") -
    // every group after the first is exactly 3 digits, which a genuine decimal fraction essentially
    // never is in this game's UI (see GetParsedDoubleAbbreviated).
    private static readonly Regex DotGroupedInteger = new(@"^\d{1,3}(\.\d{3})+$", RegexOptions.Compiled);

    /// <summary>
    ///     Parses compact/abbreviated numbers (e.g. "86,27M", "10.916.942.093,4") into a double.
    ///     Tries invariant and current culture first, then falls back to European-style grouping
    ///     ('.' as thousands separator, ',' as decimal separator).
    /// </summary>
    public double GetParsedDoubleAbbreviated(double fallback = 0)
    {
        var text = GetParsedText().Trim();
        if (text.Length == 0) return fallback;

        // Live-confirmed, 2026-09-18 (Arena of Kings' own power counter): unlike the opponents' bare
        // numbers, this one bakes a label into the same text component (e.g. "Arena power: 18.336")
        // - every parse attempt below would otherwise fail outright on the leading letters. Strips
        // everything up to and including the last ':' first, same idea as GetParsedLeadingInt
        // stripping everything after a '/'.
        var colonIndex = text.LastIndexOf(':');
        if (colonIndex >= 0) text = text[(colonIndex + 1)..].Trim();

        var multiplier = 1d;
        var suffix = char.ToUpperInvariant(text[^1]);
        if (suffix is 'K' or 'M' or 'B' or 'T')
        {
            multiplier = suffix switch { 'K' => 1e3, 'M' => 1e6, 'B' => 1e9, _ => 1e12 };
            text = text[..^1].Trim();
        }

        // Live-confirmed, 2026-09-18 (Arena of Kings power, e.g. "17.242"): a plain '.'-grouped
        // integer parses "successfully" under invariant culture too, as a tiny decimal ("17.242" ->
        // 17.242 instead of 17242) - checked first so the correct grouped reading isn't shadowed by
        // that technically-valid-but-wrong parse below.
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

    public void SetColor(Color newColor)
    {
        if (TryGetComponent(out TMP_Text tmp))
            tmp.color = newColor;
    }

    public void SetOutline(Color color, float thickness = 0.2f)
    {
        if (!TryGetComponent(out TMP_Text tmp) || tmp.fontSharedMaterial == null)
        {
            Debug($"[FAILED] FAILED to set outline: TMP_Text or Material missing. Path: {Path}");
            return;
        }

        tmp.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
        tmp.outlineColor = color;
        tmp.outlineWidth = thickness;
        tmp.UpdateMeshPadding();
        tmp.SetAllDirty();
    }

    public void RemoveOutline()
    {
        if (TryGetComponent(out TMP_Text tmp) && tmp.fontSharedMaterial != null)
        {
            tmp.outlineWidth = 0f;
            tmp.fontSharedMaterial.DisableKeyword("OUTLINE_ON");
            tmp.UpdateMeshPadding();
            tmp.SetAllDirty();
        }
    }
}
