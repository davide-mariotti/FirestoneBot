using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Shared;

/// <summary>Quick-access badges on the battle screen's notification rail. Grown as needed.</summary>
public static class Notifications
{
    // See UiVariantButton - this whole rail lives under one of two alternate HUD roots depending
    // on the client, only one populated per session. Every badge below tries both.
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

    // Not independently verified (see Battle.cs comment on TemplePrestige) - never used a
    // notification for this feature at all.
    public static IEnumerator TemplePrestige => Click(Paths.BattleLoc.NotificationsLoc.TemplePrestige);

    // Not independently verified (see Battle.cs comment on MeteoriteResearch) - never implemented
    // this feature at all.
    public static IEnumerator MeteoriteResearch => Click(Paths.BattleLoc.NotificationsLoc.MeteoriteResearch);

    // No prior precedent (Scarab Game was never automated before), but both confirmed present on the live
    // rail via UnityPy (see Battle.cs). ScarabGame opens the mini-game screen; ScarabGameShopFreeToken
    // is presumed to open ScarabGameShop directly (same "notification skips straight to the target"
    // pattern as OraclesGift bypassing Store) - unverified, flagged in the task itself.
    public static IEnumerator ScarabGame => Click(Paths.BattleLoc.NotificationsLoc.ScarabGame);

    public static IEnumerator ScarabGameShopFreeToken =>
        Click(Paths.BattleLoc.NotificationsLoc.ScarabGameShopFreeToken);

    // Sourced only from the static doc scan (see Battle.cs) - no prior precedent, not independently
    // verified via UnityPy.
    public static IEnumerator ArcaneCrystal => Click(Paths.BattleLoc.NotificationsLoc.ArcaneCrystal);

    // Confirmed present via UnityPy (see Battle.cs). No prior precedent.
    public static IEnumerator BeerExchange => Click(Paths.BattleLoc.NotificationsLoc.BeerExchange);

    // Confirmed present via UnityPy (see Battle.cs). No prior precedent.
    public static IEnumerator TalentAvailable => Click(Paths.BattleLoc.NotificationsLoc.TalentAvailable);

    // Confirmed present via UnityPy (see Battle.cs). No prior precedent. Opportunistic fast path only,
    // not used as any task's NotificationPath - see ArenaOfKingsTask.
    public static IEnumerator ArenaTokens => Click(Paths.BattleLoc.NotificationsLoc.ArenaTokens);

    // Confirmed present via UnityPy (see Battle.cs). No prior precedent. Opportunistic fast path only -
    // the real entry point is Town -> hallOfHeroes building icon (see HallOfHeroesGearTask).
    public static IEnumerator HallOfHeroes => Click(Paths.BattleLoc.NotificationsLoc.HallOfHeroes);

    // Not independently verified (see Battle.cs comment on PiratesPrize) - never implemented this
    // feature at all. Opportunistic fast path only - the real entry point is Town -> pirateShip
    // building icon (see PiratesPrizeTask).
    public static IEnumerator PiratesPrize => Click(Paths.BattleLoc.NotificationsLoc.PiratesPrize);

    // Live-confirmed real (see Battle.cs). Opportunistic fast path only - the real entry point is
    // Town -> Guild -> chaosRift/forbiddenKnowledge icon (see ChaosRiftTask/ForbiddenKnowledgeTask).
    public static IEnumerator ChaosRift => Click(Paths.BattleLoc.NotificationsLoc.ChaosRift);

    public static IEnumerator ChaosRiftSupplies => Click(Paths.BattleLoc.NotificationsLoc.ChaosRiftSupplies);

    public static IEnumerator ForbiddenKnowledge => Click(Paths.BattleLoc.NotificationsLoc.ForbiddenKnowledge);

    public static IEnumerator Awakening => Click(Paths.BattleLoc.NotificationsLoc.Awakening);

    public static IEnumerator Warmachines => Click(Paths.BattleLoc.NotificationsLoc.Warmachines);

    public static IEnumerator PharaohsVault => Click(Paths.BattleLoc.NotificationsLoc.PharaohsVault);

    public static IEnumerator ScarabGameMilestones => Click(Paths.BattleLoc.NotificationsLoc.ScarabGameMilestones);

    public static IEnumerator GuardianHolyUpgrade => Click(Paths.BattleLoc.NotificationsLoc.GuardianHolyUpgrade);
}
