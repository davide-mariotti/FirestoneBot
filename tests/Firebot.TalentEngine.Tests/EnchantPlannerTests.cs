using System.Linq;
using Firebot.Tasks.Character;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// EnchantPlanner is linked in from src/Tasks/Character: it has no game dependencies.
public class EnchantPlannerTests
{
    private const int L = HeroState.Locked;

    private static HeroState Hero(string name, bool inFormation, int power, int[] gear, int[]? jewels = null) =>
        new(name, inFormation, power, gear, jewels ?? new[] { 0, 0, 0, L, L, L });

    [Fact]
    public void Plan_BuysExactlyWhatTheBalanceCovers()
    {
        var hero = Hero("A", false, 0, new[] { 16, 16, 16, 16, 16, 16, 16, 16 }, new[] { 0, 16, 16, 0, 16, 16 });

        var plan = EnchantPlanner.Plan(new[] { hero }, EnchantCategory.Jewels, 95);

        Assert.Equal(new long[] { 30, 60 }, plan.Select(s => s.Cost));
        Assert.Equal(new[] { 0, 3 }, plan.Select(s => s.Slot));
    }

    [Fact]
    public void Plan_StopsAtTheBestCandidateInsteadOfFallingBack()
    {
        // Ring at 0 (effective -1, 120) beats the Weapon at 0 (effective 1, 30).
        var hero = Hero("A", true, 0, new[] { 0, 16, 16, 16, 16, 16, 0, 16 });

        Assert.Empty(EnchantPlanner.Plan(new[] { hero }, EnchantCategory.Gear, 100));
    }

    [Fact]
    public void Plan_RingOutranksWristByOneLevel()
    {
        var sameLevel = Hero("A", false, 0, new[] { 16, 16, 16, 2, 16, 16, 2, 16 });
        Assert.Equal(6, EnchantPlanner.Plan(new[] { sameLevel }, EnchantCategory.Gear, 100000)[0].Slot);

        var ringAhead = Hero("A", false, 0, new[] { 16, 16, 16, 2, 16, 16, 3, 16 });
        Assert.Equal(3, EnchantPlanner.Plan(new[] { ringAhead }, EnchantCategory.Gear, 100000)[0].Slot);
    }

    [Fact]
    public void TierOne_OutsideTheFormationOnlyWhileUnlockingATier()
    {
        var bench = Hero("A", false, 900, new[] { 0, 0, 0, L, L, L, L, L });

        Assert.Empty(EnchantPlanner.Plan(new[] { bench }, EnchantCategory.Gear, 100000));
        Assert.Equal(new[] { bench }, EnchantPlanner.GatingOrder(new[] { bench }));
        Assert.Equal(0, EnchantPlanner.NextGatingSlot(bench)!.Slot);
    }

    [Fact]
    public void GatingOrder_ClosestToItsThresholdFirst()
    {
        var far = Hero("Far", false, 100, new[] { 0, 0, 0, L, L, L, L, L });
        var near = Hero("Near", false, 6500, new[] { 5, 5, 5, 5, 5, 5, L, L });
        var done = Hero("Done", false, 1400, new[] { 5, 5, 5, L, L, L, L, L });

        Assert.Equal(new[] { near, far }, EnchantPlanner.GatingOrder(new[] { far, near, done }));
    }

    [Theory]
    [InlineData(1, 0, 30)]
    [InlineData(3, 3, 960)]
    [InlineData(1, 8, 5760)]
    [InlineData(2, 12, 77760)]
    [InlineData(1, 16, -1)]
    public void Cost_FollowsTheWikiTable(int tier, int level, long cost)
    {
        Assert.Equal(cost, EnchantPlanner.Cost(tier, level));
    }

    [Fact]
    public void Snapshot_RoundTripsAndRejectsGarbage()
    {
        var heroes = new[]
        {
            Hero("Le|dra", true, 5400, new[] { 6, 5, 5, 4, 4, 3, L, L }, new[] { 3, 3, 2, 2, 2, HeroState.Unavailable }),
            Hero("Talia", false, 1250, new[] { 2, 1, 1, L, L, L, L, L })
        };

        var text = EnchantPlanner.Format(heroes);
        Assert.Equal("Ledra*@5400:6,5,5,4,4,3,L,L;3,3,2,2,2,M|Talia@1250:2,1,1,L,L,L,L,L;0,0,0,L,L,L", text);
        Assert.Equal(text, EnchantPlanner.Format(EnchantPlanner.Parse(text)!));

        Assert.Null(EnchantPlanner.Parse(""));
        Assert.Null(EnchantPlanner.Parse("Ledra*@5400:6,5,5,4,4,3,L;3,3,2,2,2,1"));
        Assert.Null(EnchantPlanner.Parse("Ledra@x:6,5,5,4,4,3,L,L;3,3,2,2,2,1"));
    }
}
