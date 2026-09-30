using Firebot.Tasks.Events;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// ChallengeParser is linked in from src/Tasks/Events: it has no game dependencies.
public class ChallengeParserTests
{
    [Theory]
    // Seen live on Steam-0 (30/09).
    [InlineData("Hit the arcane crystal 10 times.", ChallengeKind.CrystalHits)]
    [InlineData("Play 12 times with the cards at the tavern.", ChallengeKind.TavernPlays)]
    [InlineData("Complete 1 meteorite researches.", ChallengeKind.MeteoriteResearches)]
    [InlineData("Enlighten guardians 1 times.", ChallengeKind.EnlightenGuardians)]
    // The wiki's mini-event texts.
    [InlineData("Play 5 times in the tavern", ChallengeKind.TavernPlays)]
    [InlineData("Open 2 chests", ChallengeKind.OpenChests)]
    [InlineData("Sell 5 items at the exotic merchant", ChallengeKind.SellItems)]
    [InlineData("Complete 1 upgrades at your personal tree of life", ChallengeKind.TreeOfLifeUpgrades)]
    [InlineData("Complete 1 exotic upgrades", ChallengeKind.ExoticUpgrades)]
    [InlineData("Donate 500 guild coins to your guild", ChallengeKind.GuildDonation)]
    // Done by the tasks already running.
    [InlineData("Complete 6 firestone researches.", ChallengeKind.NotHandled)]
    [InlineData("Get 50 special upgrades .", ChallengeKind.NotHandled)]
    public void Parse_RecognizesTheKind(string text, ChallengeKind kind) =>
        Assert.Equal(kind, ChallengeParser.Parse(text, "0/1").Kind);

    [Fact]
    public void Parse_ReadsWhatIsMissing()
    {
        var hits = ChallengeParser.Parse("Hit the arcane crystal 10 times.", "5/10");

        Assert.Equal(new Challenge(ChallengeKind.CrystalHits, 5, 10), hits);
        Assert.Equal(5, hits.Missing);
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("")]
    [InlineData("12/10")]
    public void Parse_NothingMissingWhenDoneOrUnreadable(string progress) =>
        Assert.Equal(0, ChallengeParser.Parse("Hit the arcane crystal 10 times.", progress).Missing);
}
