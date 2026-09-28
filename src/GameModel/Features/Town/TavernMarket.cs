using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town;

public static class TavernMarket
{
    public static GameButton BuyFiveTokensWithBeerBtn => new(Paths.TavernMarketLoc.BuyFiveTokensWithBeerBtn);

    public static IEnumerator Close => new GameButton(Paths.TavernMarketLoc.CloseBtn).Click();
}
