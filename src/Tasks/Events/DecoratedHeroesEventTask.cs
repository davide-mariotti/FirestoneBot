using System.Collections;
using Firebot.GameModel.Features.Events;

namespace Firebot.Tasks.Events;

/// <summary>
///     Decorated Heroes: claims the challenges, then spends the Stars of Recognition on the exchange.
///     Medals, Skins and Market are never touched.
/// </summary>
public class DecoratedHeroesEventTask : EventTask
{
    protected override string EventName => "Decorated heroes";

    protected override bool IsScreenVisible => DecoratedHeroesShop.IsVisible;

    protected override IEnumerator RunEvent()
    {
        yield return DecoratedHeroesShop.OpenChallengesTab;
        yield return DecoratedHeroesShop.ClaimAllChallenges();

        yield return DecoratedHeroesShop.OpenExchangeTab;
        yield return DecoratedHeroesShop.Exchange.BuyPriorityItems();

        yield return DecoratedHeroesShop.Close;
    }
}
