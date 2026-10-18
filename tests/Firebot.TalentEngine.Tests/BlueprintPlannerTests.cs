using Firebot.Tasks.Town;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// BlueprintPlanner is linked in from src/Tasks/Town: it has no game dependencies.
public class BlueprintPlannerTests
{
    // The game's Specialization and UpgradeCode values.
    private const int DamageRole = 0, Tank = 1, Healer = 2;
    private const int Dmg = 0, Hp = 1, Arm = 2;

    private static BlueprintOption Option(string code, int role, int attribute, int level, bool inSquad = true) =>
        new(code, code, role, attribute, level, 100 + 5 * level, inSquad);

    [Fact]
    public void Next_TakesEachRolesMainAttributeFirst_LowestLevelFirst_SquadFirst()
    {
        var open = new[]
        {
            Option("tank", Tank, Dmg, 0), Option("tank", Tank, Arm, 4),
            Option("dps", DamageRole, Arm, 0), Option("dps", DamageRole, Dmg, 2),
            Option("heal", Healer, Hp, 0), Option("heal", Healer, Dmg, 3),
            Option("bench", DamageRole, Dmg, 0, inSquad: false)
        };

        Assert.Equal(("dps", Dmg), Pick(open));
        Assert.Equal(("tank", Arm), Pick(new[] { open[0], open[1], open[4], open[6] }));
        Assert.Equal(("heal", Hp), Pick(new[] { open[0], open[2], open[4], open[6] }));
        Assert.Equal(("bench", Dmg), Pick(new[] { open[6] }));
        Assert.Null(BlueprintPlanner.Next(new BlueprintOption[0]));
    }

    private static (string, int) Pick(BlueprintOption[] open)
    {
        var next = BlueprintPlanner.Next(open)!;
        return (next.Code, next.Attribute);
    }
}
