namespace Firebot.Infrastructure;

/// <summary>Exchanges beer for game tokens; opened from the Tavern.</summary>
public static partial class Paths
{
    public static class TavernMarketLoc
    {
        private const string Root = MenusLoc.Root + "/popups/TavernMarket";

        public const string CloseBtn = Root + "/bg/closeButton";

        private const string ItemsRoot = Root + "/bg/items";

        // The only beer-priced offer. Its neighbours gameTokenBulk (x20) and gameToken (x5) cost
        // GEMS - never click those.
        public const string BuyFiveTokensWithBeerBtn = ItemsRoot + "/gameTokenWithBeer/purchaseButtonOffer";
    }
}
