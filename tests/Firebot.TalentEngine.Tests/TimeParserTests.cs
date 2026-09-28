using System;
using Firebot.Utilities;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// TimeParser is linked in from src/Utilities: it has no game dependencies.
public class TimeParserTests
{
    [Theory]
    [InlineData("6d 12:30:15", 6, 12, 30, 15)]
    [InlineData("Time played: 1:02:03", 0, 1, 2, 3)]
    [InlineData("Ends in 12:30", 0, 0, 12, 30)]
    [InlineData("2D 00:00:05", 2, 0, 0, 5)]
    public void ParseFrom_FindsTheCountdownInText(string text, int days, int hours, int minutes, int seconds)
    {
        Assert.Equal(new TimeSpan(days, hours, minutes, seconds), TimeParser.ParseFrom(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Claim")]
    public void NoCountdown_IsZeroAndMinValue(string text)
    {
        Assert.Equal(TimeSpan.Zero, TimeParser.ParseFrom(text));
        Assert.Equal(DateTime.MinValue, TimeParser.ParseExpectedTime(text));
    }

    [Fact]
    public void FormatFriendlyDuration_FoldsDaysIntoHoursAndSkipsZeroUnits()
    {
        Assert.Equal("26h 4s", TimeParser.FormatFriendlyDuration(new TimeSpan(1, 2, 0, 4)));
        Assert.Equal("0s", TimeParser.FormatFriendlyDuration(TimeSpan.FromSeconds(-5)));
    }
}
