using Firebot.Tasks.Town;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// GuardianPlanner is linked in from src/Tasks/Town: it has no game dependencies.
public class GuardianPlannerTests
{
    private static GuardianState G(string name, int evolution, bool unlocked = true, bool ready = false) =>
        new(name, unlocked, evolution, 1, ready);

    private static string? Pick(params GuardianState[] guardians) => GuardianPlanner.Pick(guardians)?.Name;

    [Fact]
    public void Pick_TakesEachToFiveInOrder_ThenEachToTen_WhateverTheSlots()
    {
        // Slots as Steam-17 showed them (26/11): Grace first.
        Assert.Equal("Vermilion", Pick(G("Grace", 1), G("Vermilion", 3), G("Ankaa", 1, false), G("Azhar", 1, false)));
        Assert.Equal("Grace", Pick(G("Grace", 1), G("Vermilion", 5), G("Ankaa", 1, false), G("Azhar", 1, false)));
        Assert.Equal("Ankaa", Pick(G("Grace", 5), G("Vermilion", 5), G("Ankaa", 1), G("Azhar", 1, false)));
        // Azhar unlocks on the way to 10: he goes to 5 first.
        Assert.Equal("Azhar", Pick(G("Grace", 5), G("Vermilion", 7), G("Ankaa", 5), G("Azhar", 1)));
        Assert.Equal("Vermilion", Pick(G("Grace", 5), G("Vermilion", 7), G("Ankaa", 5), G("Azhar", 5)));
        Assert.Equal("Ankaa", Pick(G("Grace", 10), G("Vermilion", 10), G("Ankaa", 9), G("Azhar", 5)));
        Assert.Null(Pick(G("Grace", 10), G("Vermilion", 10), G("Ankaa", 10), G("Azhar", 10)));
    }

    [Fact]
    public void Pick_SkipsOneReadyToEvolve()
    {
        Assert.Equal("Grace", Pick(G("Vermilion", 4, ready: true), G("Grace", 1), G("Ankaa", 1, false)));
        Assert.Null(Pick(G("Vermilion", 4, ready: true)));
    }
}
