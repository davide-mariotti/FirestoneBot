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

    /// <summary>A game-formatted number; see StringUtils.ParseAbbreviated.</summary>
    public double GetParsedDoubleAbbreviated(double fallback = 0) =>
        StringUtils.ParseAbbreviated(GetParsedText(), fallback);

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
