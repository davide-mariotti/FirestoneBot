using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Turns accumulated beer into Tavern game tokens, 5 at a time, on the BeerExchange badge and every
///     2 hours - so GamerQuestTask always has tokens for its draws. Only the beer-priced offer is used.
/// </summary>
public class BeerExchangeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;
    protected override int MinimumCharacterLevel => 15;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(2);

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.BeerExchange;

    public override IEnumerator Execute()
    {
        // The 2-hour check: the BeerExchange badge is the game's own "the beer pays an exchange"
        // (NotificationHandler.BeerExchangeCheck), read from its data. Not lit, nothing to buy.
        if (GameState.BadgeType(Paths.BattleLoc.NotificationsLoc.BeerExchange) is { } type && GameState.HasBadge(type) == false)
        {
            Debug($"[INFO] From the game: {GameState.Currency(Il2Cpp.CurrencyCode.Beer):0} beer, no exchange it pays - not opening.");
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return Notifications.BeerExchange;

        yield return TownScreen.Open;
        yield return TownScreen.OpenTavern;
        yield return Tavern.OpenMarket;

        var buyBtn = TavernMarket.BuyFiveTokensWithBeerBtn;
        while (buyBtn.IsClickable()) yield return buyBtn.Click();

        yield return TavernMarket.Close;
        yield return Tavern.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
