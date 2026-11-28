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
///     Claims the free daily item in the Scarab's Game shop's sale tab. The shop's Monthly pass tab has
///     a free claim of its own that isn't handled yet.
/// </summary>
public class ScarabGameFreeTokenTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.ScarabGame;
    protected override int MinimumCharacterLevel => 60;

    protected override string DisplayName => "Game Free Token";

    private static readonly TimeSpan FallbackRetryDelay = TimeSpan.FromHours(6);

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ScarabGameShopFreeToken;

    public override IEnumerator Execute()
    {
        // The game knows whether today's free token is still there.
        if (!IsNotificationVisible() &&
            GameState.Get("scarab free token", () => GameState.H.ScarabGameHandler.DailyFreePharaohTokenAvailable) == false)
        {
            Debug("[INFO] From the game: today's free token is taken - not opening.");
            NextRunTime = GameState.NextGameDay ?? DateTime.Now + FallbackRetryDelay;
            yield break;
        }

        yield return Notifications.ScarabGameShopFreeToken;
        yield return Notifications.ScarabGame;

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
