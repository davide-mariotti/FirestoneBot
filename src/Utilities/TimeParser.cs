using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Firebot.Utilities;

/// <summary>Parses the game's countdown texts ("6d 12:30:15", "12:30", "9:28").</summary>
public static class TimeParser
{
    private static readonly Regex TimeRegex = new(
        @"(?:(?<days>\d+)d\s*)?(?:(?<h>\d+):(?<m>\d+):(?<s>\d+)|(?<m_only>\d+):(?<s_only>\d+))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>The first duration anywhere in text, other words around it allowed; TimeSpan.Zero if none.</summary>
    public static TimeSpan ParseFrom(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return TimeSpan.Zero;

        var match = TimeRegex.Match(raw);
        if (!match.Success) return ParseUnits(raw);

        int Group(string name) => match.Groups[name].Success ? int.Parse(match.Groups[name].Value) : 0;

        return match.Groups["h"].Success
            ? new TimeSpan(Group("days"), Group("h"), Group("m"), Group("s"))
            : new TimeSpan(Group("days"), 0, Group("m_only"), Group("s_only"));
    }

    // "2d 5h", "10h 3m", "3 days": the Temple's adventure time on Steam-17..31 never matched the colon
    // form (always 00:00:00 from 17/10), so the 60-min minimum blocked every Empower there.
    private static readonly Regex UnitRegex = new(@"(?<n>\d+)\s*(?<u>[dhms])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static TimeSpan ParseUnits(string raw)
    {
        var total = TimeSpan.Zero;
        foreach (Match m in UnitRegex.Matches(raw))
        {
            var n = int.Parse(m.Groups["n"].Value);
            total += char.ToLowerInvariant(m.Groups["u"].Value[0]) switch
            {
                'd' => TimeSpan.FromDays(n),
                'h' => TimeSpan.FromHours(n),
                'm' => TimeSpan.FromMinutes(n),
                _ => TimeSpan.FromSeconds(n)
            };
        }

        return total;
    }

    /// <summary>Now + the countdown the text shows, or DateTime.MinValue when it shows none.</summary>
    public static DateTime ParseExpectedTime(string raw)
    {
        var duration = ParseFrom(raw);
        return duration == TimeSpan.Zero ? DateTime.MinValue : DateTime.Now.Add(duration);
    }

    /// <summary>"1h 2m 3s", omitting zero units; negative spans read as "0s".</summary>
    public static string FormatFriendlyDuration(TimeSpan span)
    {
        if (span.TotalSeconds < 0)
            span = TimeSpan.Zero;

        var parts = new List<string>();
        if (span.Hours > 0 || span.Days > 0)
            parts.Add($"{span.Days * 24 + span.Hours}h");
        if (span.Minutes > 0)
            parts.Add($"{span.Minutes}m");
        if (span.Seconds > 0 || parts.Count == 0)
            parts.Add($"{span.Seconds}s");
        return string.Join(" ", parts);
    }
}
