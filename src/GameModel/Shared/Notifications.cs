using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Shared;

/// <summary>
///     Clicks on the notification rail's badges. A badge opens its screen directly, so tasks click
///     theirs first as a shortcut and then navigate the full path anyway - a hidden badge makes the
///     click a no-op, and the explicit navigation never depends on the shortcut having worked.
/// </summary>
public static class Notifications
{
    private static IEnumerator Click(string badgeName) => UiVariantButton.Click(
        new GameNotificationButton(Paths.BattleLoc.NotificationsLoc.Root + "/" + badgeName),
        new GameNotificationButton(Paths.BattleLoc.NotificationsLoc.FallbackRoot + "/" + badgeName));

    public static IEnumerator OraclesGift => Click(Paths.BattleLoc.NotificationsLoc.OraclesGift);

    public static IEnumerator CheckIn => Click(Paths.BattleLoc.NotificationsLoc.CheckIn);

    public static IEnumerator MysteryBox => Click(Paths.BattleLoc.NotificationsLoc.MysteryBox);

    public static IEnumerator Quests => Click(Paths.BattleLoc.NotificationsLoc.Quests);

    public static IEnumerator FreePickaxes => Click(Paths.BattleLoc.NotificationsLoc.FreePickaxes);

    public static IEnumerator Engineer => Click(Paths.BattleLoc.NotificationsLoc.Engineer);

    public static IEnumerator Expeditions => Click(Paths.BattleLoc.NotificationsLoc.Expeditions);

    public static IEnumerator GuardianTraining => Click(Paths.BattleLoc.NotificationsLoc.GuardianTraining);

    public static IEnumerator OracleRituals => Click(Paths.BattleLoc.NotificationsLoc.OracleRituals);

    public static IEnumerator Experiments => Click(Paths.BattleLoc.NotificationsLoc.Experiments);

    public static IEnumerator WarfrontCampaign => Click(Paths.BattleLoc.NotificationsLoc.WarfrontCampaign);

    public static IEnumerator MapMissions => Click(Paths.BattleLoc.NotificationsLoc.MapMissions);

    public static IEnumerator FirestoneResearch => Click(Paths.BattleLoc.NotificationsLoc.FirestoneResearch);

    public static IEnumerator TemplePrestige => Click(Paths.BattleLoc.NotificationsLoc.TemplePrestige);

    public static IEnumerator MeteoriteResearch => Click(Paths.BattleLoc.NotificationsLoc.MeteoriteResearch);

    public static IEnumerator ScarabGame => Click(Paths.BattleLoc.NotificationsLoc.ScarabGame);

    public static IEnumerator ScarabGameShopFreeToken =>
        Click(Paths.BattleLoc.NotificationsLoc.ScarabGameShopFreeToken);

    public static IEnumerator ArcaneCrystal => Click(Paths.BattleLoc.NotificationsLoc.ArcaneCrystal);

    public static IEnumerator BeerExchange => Click(Paths.BattleLoc.NotificationsLoc.BeerExchange);

    public static IEnumerator TalentAvailable => Click(Paths.BattleLoc.NotificationsLoc.TalentAvailable);

    public static IEnumerator ArenaTokens => Click(Paths.BattleLoc.NotificationsLoc.ArenaTokens);

    public static IEnumerator HallOfHeroes => Click(Paths.BattleLoc.NotificationsLoc.HallOfHeroes);

    public static IEnumerator PiratesPrize => Click(Paths.BattleLoc.NotificationsLoc.PiratesPrize);

    public static IEnumerator ChaosRift => Click(Paths.BattleLoc.NotificationsLoc.ChaosRift);

    public static IEnumerator ChaosRiftSupplies => Click(Paths.BattleLoc.NotificationsLoc.ChaosRiftSupplies);

    public static IEnumerator ForbiddenKnowledge => Click(Paths.BattleLoc.NotificationsLoc.ForbiddenKnowledge);

    public static IEnumerator Awakening => Click(Paths.BattleLoc.NotificationsLoc.Awakening);

    public static IEnumerator Warmachines => Click(Paths.BattleLoc.NotificationsLoc.Warmachines);

    public static IEnumerator ScarabGameMilestones => Click(Paths.BattleLoc.NotificationsLoc.ScarabGameMilestones);

    public static IEnumerator GuardianHolyUpgrade => Click(Paths.BattleLoc.NotificationsLoc.GuardianHolyUpgrade);

    public static IEnumerator GuardianEvolution => Click(Paths.BattleLoc.NotificationsLoc.GuardianEvolution);

    public static IEnumerator WarMachinesRarity => Click(Paths.BattleLoc.NotificationsLoc.WarMachinesRarity);

    public static IEnumerator ScarabGameBeastRelease => Click(Paths.BattleLoc.NotificationsLoc.ScarabGameBeastRelease);
}
