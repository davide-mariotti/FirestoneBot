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
}
