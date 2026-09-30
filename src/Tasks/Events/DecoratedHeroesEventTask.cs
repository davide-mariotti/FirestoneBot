using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Features.Events;

namespace Firebot.Tasks.Events;

/// <summary>
///     Decorated Heroes: claims the challenges, then spends the Stars of Recognition on the exchange.
///     Medals, Skins and Market are never touched.
/// </summary>
public class DecoratedHeroesEventTask : EventTask
{
    protected override string[] EventNames => new[] { "Decorated heroes" };

    protected override bool IsScreenVisible => DecoratedHeroesShop.IsVisible;

    protected override IEnumerator RunEvent(List<(string Text, string Progress)> challenges)
    {
        yield return DecoratedHeroesShop.OpenChallengesTab;
        yield return DecoratedHeroesShop.ClaimAllChallenges();
        challenges.AddRange(DecoratedHeroesShop.Challenges());

        yield return DecoratedHeroesShop.OpenExchangeTab;
        yield return DecoratedHeroesShop.Exchange.BuyPriorityItems();

        yield return DecoratedHeroesShop.Close;
    }
}
