using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town;

public static class TavernMarket
{
    // See Paths.TavernMarketLoc doc comment - assumed (not confirmed) to be the beer-priced option.
    public static IEnumerator BuyFiveTokensWithBeer =>
        new GameButton(Paths.TavernMarketLoc.BuyFiveTokensWithBeerBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.TavernMarketLoc.CloseBtn).Click();
}
