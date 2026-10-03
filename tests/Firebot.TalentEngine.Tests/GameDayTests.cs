using System;
using Firebot.Utilities;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// GameDay is linked in from src/Utilities: it has no game dependencies.
public class GameDayTests
{
    private static readonly DateTime Day = new(2026, 10, 4);

    [Theory]
    [InlineData(9, 59, 45)] // a countdown read a few seconds early
    [InlineData(10, 0, 0)]
    [InlineData(10, 1, 59)]
    public void PastReset_MovesTimesOnTheResetTo1002(int h, int m, int s) =>
        Assert.Equal(Day.AddHours(10).AddMinutes(2), GameDay.PastReset(Day.Add(new TimeSpan(h, m, s))));

    [Theory]
    [InlineData(9, 59, 0)]
    [InlineData(10, 2, 0)]
    [InlineData(16, 30, 0)]
    public void PastReset_LeavesOtherTimesAlone(int h, int m, int s)
    {
        var time = Day.Add(new TimeSpan(h, m, s));
        Assert.Equal(time, GameDay.PastReset(time));
    }

    [Fact]
    public void PastReset_LeavesTheNeverAgainMarkersAlone()
    {
        Assert.Equal(DateTime.MaxValue, GameDay.PastReset(DateTime.MaxValue));
        Assert.Equal(DateTime.MinValue, GameDay.PastReset(DateTime.MinValue));
    }
}
