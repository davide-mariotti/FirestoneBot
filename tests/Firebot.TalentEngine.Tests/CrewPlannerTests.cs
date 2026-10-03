using System.Collections.Generic;
using Firebot.Tasks.Map;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// CrewPlanner is linked in from src/Tasks/Map: it has no game dependencies.
public class CrewPlannerTests
{
    // The game's Specialization values.
    private const int Damage = 0, Tank = 1, Healer = 2;

    private static CrewSpot Spot(int role, params int[] crew) => new(role, crew);

    [Fact]
    public void Plan_SwapsHeroesIntoTheMachineOfTheirSpecialization()
    {
        var spots = new[] { Spot(Tank, 1), Spot(Damage, 2) };
        var heroes = new Dictionary<int, int> { [1] = Damage, [2] = Tank };

        var target = CrewPlanner.Plan(spots, heroes, 4);

        Assert.Equal(new[] { 2 }, target[0]);
        Assert.Equal(new[] { 1 }, target[1]);
        Assert.Equal(2, CrewPlanner.Moves(spots, target));
    }

    [Fact]
    public void Plan_WithoutMatchingHeroes_KeepsTheCrewsAndFillsTheSmallestFirst()
    {
        // No Tank or Healer hero at all: nobody moves, the free one joins the smallest crew.
        var spots = new[] { Spot(Tank, 1, 2), Spot(Damage, 3), Spot(Healer) };
        var heroes = new Dictionary<int, int> { [1] = Damage, [2] = Damage, [3] = Damage, [4] = Damage };

        var target = CrewPlanner.Plan(spots, heroes, 4);

        Assert.Equal(new[] { 1, 2 }, target[0]);
        Assert.Equal(new[] { 3 }, target[1]);
        Assert.Equal(new[] { 4 }, target[2]);
    }

    [Fact]
    public void Plan_KeepsCrewSizesAndLeavesMatchedCrewsAlone()
    {
        // The tank keeps its 3 slots and its Tank hero; the Healer hero moves to the healer, and the
        // Damage hero there takes its place.
        var spots = new[] { Spot(Tank, 1, 2, 3), Spot(Healer, 4) };
        var heroes = new Dictionary<int, int> { [1] = Tank, [2] = Damage, [3] = Healer, [4] = Damage };

        var target = CrewPlanner.Plan(spots, heroes, 4);

        Assert.Equal(new[] { 1, 2, 4 }, target[0]);
        Assert.Equal(new[] { 3 }, target[1]);
    }

    [Fact]
    public void Plan_DealsFreeHeroesToEmptyCrewsByRole()
    {
        // After the machines change every crew is empty: 2 each, Tank heroes on the tank.
        var spots = new[] { Spot(Tank), Spot(Damage), Spot(Healer) };
        var heroes = new Dictionary<int, int> { [1] = Damage, [2] = Damage, [3] = Tank, [4] = Healer, [5] = Damage, [6] = Tank };

        var target = CrewPlanner.Plan(spots, heroes, 4);

        Assert.Equal(new[] { 3, 6 }, target[0]);
        Assert.Equal(new[] { 1, 2 }, target[1]);
        Assert.Equal(new[] { 4, 5 }, target[2]);
    }
}
