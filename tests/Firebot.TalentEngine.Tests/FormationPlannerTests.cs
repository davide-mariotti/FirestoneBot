using System.Linq;
using Firebot.Tasks.Character;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// FormationPlanner is linked in from src/Tasks/Character: it has no game dependencies.
public class FormationPlannerTests
{
    // Steam-0 on 23/10 at 15:03, log10 (offense from the probe, potential = base damage + the three
    // multipliers, both with attack speed and crit).
    private static readonly HeroStats[] Steam0 =
    {
        new("Boris", true, 186.11, 23.06), new("Solaine", false, 186.62, 23.16), new("Talia", true, 187.96, 25.16),
        new("Burt", false, 185.30, 24.82), new("Benedictus", false, 183.61, 22.40), new("Muriel", true, 186.72, 25.04),
        new("Blaze", false, 183.77, 23.20), new("Luana", false, 181.30, 22.40), new("Valerius", true, 180.68, 20.96),
        new("Astrid", false, 184.15, 24.85), new("Ina", false, 182.02, 24.83), new("Fini", false, 181.82, 24.82),
        new("Leo", true, 181.08, 21.05), new("Cirilo", false, 186.97, 24.83)
    };

    private const double Steam0LeaderRatio = 3.36 - 2.18;

    [Fact]
    public void Best_Steam0_KeepsTheFiveButMakesTaliaLeader()
    {
        var best = FormationPlanner.Best(Steam0, h => h.Offense, Steam0LeaderRatio);

        Assert.Equal(new[] { "Boris", "Muriel", "Talia", "Solaine", "Cirilo" }, best.Slots);
        Assert.Equal("Talia", best.Leader);

        // The same five with Cirilo leading, as in battle that afternoon: ~5.7 times less damage.
        var now = FormationPlanner.Score(Steam0, best.Slots, "Cirilo", h => h.Offense, Steam0LeaderRatio);
        Assert.InRange(best.Score - now, 0.74, 0.78);
        Assert.True(FormationPlanner.Beats(best.Score, now, 0.10));
    }

    [Fact]
    public void Best_OnPotential_FollowsTheAccountsMultipliers()
    {
        // Steam-0's rage and energy multipliers are ~2 orders above mana: no mana hero in the target.
        var target = FormationPlanner.Best(Steam0, h => h.Potential, Steam0LeaderRatio);

        Assert.Equal("Talia", target.Leader);
        Assert.DoesNotContain(target.Slots, n => n is "Solaine" or "Blaze" or "Boris");
        Assert.Contains("Muriel", target.Slots);
    }

    [Fact]
    public void Best_WithTeamBonusAboveLeadership_LeadsWithTheWeakest()
    {
        var best = FormationPlanner.Best(Steam0, h => h.Offense, -0.5);

        Assert.Equal("Boris", best.Leader);
        Assert.Equal(5, best.Slots.Count);
    }

    [Fact]
    public void Beats_NeedsTheMinimumGain()
    {
        Assert.False(FormationPlanner.Beats(100.03, 100, 0.10)); // +7%
        Assert.True(FormationPlanner.Beats(100.05, 100, 0.10)); // +12%
        Assert.True(FormationPlanner.Beats(1, double.NegativeInfinity, 0.10));
        Assert.Empty(FormationPlanner.Best(Enumerable.Empty<HeroStats>(), h => h.Offense, 1).Slots);
    }
}
