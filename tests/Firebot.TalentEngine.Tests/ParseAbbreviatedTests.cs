using System;
using Firebot.Utilities;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// StringUtils is linked in from src/Utilities: it has no game dependencies.
public class ParseAbbreviatedTests
{
    [Theory]
    [InlineData("86,27M", 86.27e6)]
    [InlineData("17.242", 17242)]
    [InlineData("10.916.942.093,4", 10916942093.4)]
    [InlineData("Arena power: 18.336", 18336)]
    [InlineData("2.56aa", 2.56e15)]
    [InlineData("1.0ab", 1e18)]
    // The Temple of Eternals texts Steam-0 showed on 2026-09-29, both read as 0 before.
    [InlineData("1,79bl", 1.79e126)]
    [InlineData("65,67bl", 65.67e126)]
    public void ReadsGameNumbers(string text, double expected)
    {
        Assert.Equal(expected, StringUtils.ParseAbbreviated(text), expected * 1e-9);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Maxed")]
    public void NonNumbers_GiveTheFallback(string text)
    {
        Assert.Equal(-1, StringUtils.ParseAbbreviated(text, -1));
    }

    // Steam-0 on 05/11: both counts past a double's range, and a real ratio in the thousands.
    [Fact]
    public void Log10_ComparesNumbersPastADouble()
    {
        var found = StringUtils.ParseAbbreviatedLog10("6,35dv");
        var owned = StringUtils.ParseAbbreviatedLog10("468,24dt");

        Assert.True(double.IsInfinity(StringUtils.ParseAbbreviated("6,35dv")));
        Assert.Equal(312.803, found, 3);
        Assert.Equal(308.670, owned, 3);
        Assert.Equal(13561, Math.Pow(10, found - owned), 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("Loading")]
    public void Log10_IsMinusInfinityWithoutANumber(string text) =>
        Assert.Equal(double.NegativeInfinity, StringUtils.ParseAbbreviatedLog10(text));
}
