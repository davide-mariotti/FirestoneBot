using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using ScarabGameScreen = Firebot.GameModel.Features.ScarabGame.ScarabGame;
using ScarabGameShopScreen = Firebot.GameModel.Features.ScarabGame.ScarabGameShop;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.ScarabGame;

/// <summary>
///     Claims the free daily gift in ScarabGameShop's "Saldi" tab (named "purchaseButton" but
///     confirmed genuinely free via a sibling "freeText" label - same pattern as Task 3's mystery
///     box). Scarab Game was never automated before.
///     Reached via Town -&gt; the "tavern" building's TavernSelection choice popup -&gt; its "scarabGame"
///     card (live-confirmed 2026-09-18, see Town.OpenScarabGame) - corrected after initially assuming
///     there was no permanent manual entry point at all, only the battle-screen notification badges
///     (still used as a fast path below).
///     Level-gated like OraclesGiftTask: the wiki's Scarab's Game infobox lists "unlock = Level 60".
/// </summary>
public class ScarabGameFreeTokenTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.ScarabGame;
    protected override int MinimumCharacterLevel => 60;

    // Cosmetic only, per the user (2026-09-20) - avoids the class-name-derived "Scarab Game Free
    // Token" repeating the group name in the terminal table ("Scarab Game - Scarab Game Free Token").
    protected override string DisplayName => "Game Free Token";

    private static readonly TimeSpan FallbackRetryDelay = TimeSpan.FromHours(6);

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ScarabGameShopFreeToken;

    public override IEnumerator Execute()
    {
        // Fast paths: either badge (when up) may already open the shop directly. Safe no-ops otherwise.
        yield return Notifications.ScarabGameShopFreeToken;
        yield return Notifications.ScarabGame;

        // Guaranteed path regardless of the notification - same reasoning as every other task.
        yield return TownScreen.Open;
        yield return TownScreen.OpenScarabGame;
        yield return ScarabGameScreen.OpenShop;

        yield return ScarabGameShopScreen.OpenSaleTab;
        yield return ScarabGameShopScreen.ClaimFreeToken;

        yield return ScarabGameShopScreen.Close;
        yield return ScarabGameScreen.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + FallbackRetryDelay;
    }
}
