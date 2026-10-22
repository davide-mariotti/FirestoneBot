using Firebot.Tasks.Town;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// AmuletPlanner is linked in from src/Tasks/Town: it has no game dependencies.
public class AmuletPlannerTests
{
    private static readonly string[] AllTier1 =
        { "AmuletOfConquest", "AmuletOfWar", "AmuletOfKnowledge", "AmuletOfAlchemy" };

    private static bool Buy(string today, string[] owned, double gems, int level = 150) =>
        AmuletPlanner.Decide(today, owned, level, gems, 0).Buy;

    // The examples table of AMULETS_PLAN.md, section 3.
    [Fact]
    public void Decide_KeepsOneHigherTierCovered()
    {
        Assert.True(Buy("AmuletOfWar", new[] { "AmuletOfKnowledge", "AmuletOfAlchemy" }, 2150));
        Assert.False(Buy("AmuletOfTheSky", new[] { "AmuletOfWar", "AmuletOfKnowledge", "AmuletOfAlchemy" }, 2150));
        Assert.True(Buy("AmuletOfTheSky", new[] { "AmuletOfWar", "AmuletOfKnowledge", "AmuletOfAlchemy" }, 4300));
        Assert.True(Buy("AmuletOfAstrology", new[]
        {
            "AmuletOfConquest", "AmuletOfWar", "AmuletOfKnowledge", "AmuletOfAlchemy", "AmuletOfCartography",
            "AmuletOfTheSky", "AmuletOfExploration", "AmuletOfLiberation", "AmuletOfThePioneers",
            "AmuletOfTheQuartermaster", "AmuletOfInsight"
        }, 2150, level: 250));
        Assert.False(Buy("AmuletOfClarity", new[] { "AmuletOfConquest", "AmuletOfKnowledge", "AmuletOfAlchemy" }, 5000));
        Assert.True(Buy("AmuletOfClarity", new[] { "AmuletOfConquest", "AmuletOfKnowledge", "AmuletOfAlchemy" }, 6000));
    }

    [Fact]
    public void Decide_SkipsOwnedNeverUnknownAndLocked()
    {
        Assert.False(Buy("AmuletOfConquest", new[] { "AmuletOfConquest" }, 9000));
        Assert.False(Buy("AmuletOfMidas", AllTier1, 9000));
        Assert.False(Buy("AmuletOfLuck", AllTier1, 9000));
        Assert.False(Buy("None", AllTier1, 9000));
        Assert.False(Buy("AmuletOfAlchemy", new string[0], 9000, level: 110));
        Assert.False(Buy("AmuletOfWar", new string[0], 1999));
    }

    [Fact]
    public void Decide_IgnoresLockedHigherTiersAndHonorsTheReserve()
    {
        // At level 110 Alchemy isn't missing yet, so a tier 2 needs only its own price.
        var owned = new[] { "AmuletOfConquest", "AmuletOfWar", "AmuletOfKnowledge" };
        Assert.True(AmuletPlanner.Decide("AmuletOfCartography", owned, 110, 2000, 0).Buy);
        Assert.False(AmuletPlanner.Decide("AmuletOfCartography", owned, 110, 2400, 500).Buy);
        Assert.Equal("needs 4000 (AmuletOfConquest missing)",
            AmuletPlanner.Decide("AmuletOfTheSky", new string[0], 150, 3000, 0).Reason);
    }
}
