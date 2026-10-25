using System;
using Firebot.Utilities;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// GameDay is linked in from src/Utilities: it has no game dependencies.
public class GameDayTests
{
    // The reset is 08:00 UTC on both sides of the clock change: 10:00 local on 04/10, 09:00 on 26/10.
    private static readonly DateTime[] Days = { new(2026, 10, 4), new(2026, 10, 26) };

    private static DateTime Local(DateTime day, int h, int m, int s) =>
        new DateTime(day.Year, day.Month, day.Day, h, m, s, DateTimeKind.Utc).ToLocalTime();

    [Theory]
    [InlineData(7, 59, 45)] // a countdown read a few seconds early
    [InlineData(8, 0, 0)]
    [InlineData(8, 1, 59)]
    public void PastReset_MovesTimesOnTheResetTo0802Utc(int h, int m, int s)
    {
        foreach (var day in Days)
            Assert.Equal(Local(day, 8, 2, 0), GameDay.PastReset(Local(day, h, m, s)));
    }

    [Theory]
    [InlineData(7, 59, 0)]
    [InlineData(8, 2, 0)]
    [InlineData(14, 30, 0)]
    public void PastReset_LeavesOtherTimesAlone(int h, int m, int s)
    {
        foreach (var day in Days)
        {
            var time = Local(day, h, m, s);
            Assert.Equal(time, GameDay.PastReset(time));
        }
    }

    [Fact]
    public void PastReset_LeavesTheNeverAgainMarkersAlone()
    {
        Assert.Equal(DateTime.MaxValue, GameDay.PastReset(DateTime.MaxValue));
        Assert.Equal(DateTime.MinValue, GameDay.PastReset(DateTime.MinValue));
    }

    [Fact]
    public void NextReset_IsAt0802Utc()
    {
        var next = GameDay.NextReset().ToUniversalTime();
        Assert.Equal(new TimeSpan(8, 2, 0), next.TimeOfDay);
        Assert.InRange(next - DateTime.UtcNow, TimeSpan.Zero, TimeSpan.FromDays(1));
    }
}
