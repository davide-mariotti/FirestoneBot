namespace Firebot.Infrastructure;

/// <summary>
///     Exchanges beer or gems for game tokens, opened from Tavern via TavernLoc.OpenMarketBtn. Never
///     automated before. Originally assumed (from the wiki's two rows for the same 5-token offer) that
///     a single "gameToken" item had two payment buttons, one beer one gems, structural position only
///     (never confirmed). Live dump, 2026-09-18, showed the real structure is different: THREE separate
///     item cards - "gameTokenWithBeer" (x5, beer, its own uniquely-named "purchaseButtonOffer"),
///     "gameTokenBulk" (x20, gems, "+20% Free" ribbon) and "gameToken" (x5, gems). The old assumed path
///     ("gameToken/purchaseButton") would have clicked the GEMS-priced offer, not beer - never actually
///     triggered live only because the popup root itself was also wrong at the time.
/// </summary>
public static partial class Paths
{
    public static class TavernMarketLoc
    {
        // Live-confirmed, 2026-09-18: this is a popup, not a menu (same wrong-guess pattern as
        // BattlePass) - a generic Watchdog sweep found "closeButton" resolving fine under
        // "popups/TavernMarket" while "menus/TavernMarket/..." was entirely broken.
        private const string Root = MenusLoc.Root + "/popups/TavernMarket";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string ItemsRoot = Root + "/bg/items";

        // Live-confirmed, 2026-09-18: the only beer-priced item - it's its own separate card, not a
        // second button bolted onto the gems item.
        public const string BuyFiveTokensWithBeerBtn = ItemsRoot + "/gameTokenWithBeer/purchaseButtonOffer";

        // Deliberately never referenced by any task - both gems-priced (x20 "gameTokenBulk", x5
        // "gameToken"). Live-confirmed, 2026-09-18.
        public const string BuyTwentyTokensWithGemsBtnDoNotUse = ItemsRoot + "/gameTokenBulk/purchaseButton";
        public const string BuyFiveTokensWithGemsBtnDoNotUse = ItemsRoot + "/gameToken/purchaseButton";
    }
}
