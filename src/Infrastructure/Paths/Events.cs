namespace Firebot.Infrastructure;

/// <summary>
///     Events hub (Battle -&gt; Paths.BattleLoc.BottomSideUIMobileLoc.EventsBtn, never wired to any
///     task before this) and per-event shop screens. Only Decorated Heroes is currently confirmed
///     live (screenshots, 2026-09-23) since it's the only recurring seasonal event active on any
///     account the user has access to right now - New Player Event is a one-time onboarding event,
///     not one of the recurring seasonal ones (Halloween/Winter/Valentine's/Spring/Tropicana/Space)
///     the user wants covered long-term, and had already ended on the account first screenshotted.
///     Root mounts (EventManagerLoc, DecoratedHeroesShopLoc) live under "events/", NOT "menus/" like
///     most other full-screen menus in this codebase - the initial guess assumed the usual "menus/X"
///     convention and was wrong. Live-confirmed, 2026-09-23, via Watchdog's own generic sweep: it
///     dynamically enumerates the REAL live children of menusRoot/.../menuCanvas/events (see
///     Paths.WatchdogLoc.EventsRoot) and found "EventManager" and "DecoratedHeroesShop" listed there
///     by name - not a guess, an actual runtime child-name dump, the same kind of ground truth this
///     codebase's diagnostic dumps elsewhere are built from. (This also explains the pre-existing
///     "events/DecoratedHeroesPromotion" nuisance-popup path already seen in earlier Watchdog
///     activity - same root, a different one-time promo popup, unrelated to this event's own shop.)
///     Designed to be reusable: EventManagerLoc/EventManager (GameModel) are fully generic (any
///     event's hub card), only a per-event shop screen like DecoratedHeroesShopLoc needs to be added
///     per new event, reusing the same Challenges/Exchange shapes.
/// </summary>
public static partial class Paths
{
    public static class EventManagerLoc
    {
        public const string Root = MenusLoc.Root + "/events/EventManager";

        public const string CloseBtn = Root + "/bg/closeButton";

        // Live-confirmed via UnityPy dump, 2026-09-23: both are real VerticalLayoutGroup containers,
        // "activeEvents" for events currently joinable, "upcommingEvents" for locked/future ones
        // (sic - "upcommingEvents" is the game's own misspelling, not a typo introduced here). The
        // number of "eventInteraction" children under each is dynamic (however many events are
        // currently active/upcoming) - never assume a fixed count, scan GetChildren() instead.
        public const string ActiveEventsRoot =
            Root + "/bg/verticalLayout/Scroll View/Viewport/Content/activeEvents";

        public const string UpcomingEventsRoot =
            Root + "/bg/verticalLayout/Scroll View/Viewport/Content/upcommingEvents";

        // Relative to a card GameElement (an "eventInteraction" child of the two roots above).
        public const string CardTitleTxt = "/mainElements/title";
    }

    /// <summary>
    ///     Decorated Heroes' event shop. Live-confirmed via screenshots, 2026-09-23: 5 tabs
    ///     (Challenges/Medals/Stars exchange/Skins/Market) - per the user, only Challenges and
    ///     Exchange are ever touched here. Medals, Skins and Market (chest packs, possibly real-money
    ///     per the "Skins" naming) are deliberately never wired to anything, same convention as
    ///     ChaosRiftShopLoc's deliberately-unwired Eclipse Stone buttons.
    /// </summary>
    public static class DecoratedHeroesShopLoc
    {
        public const string Root = MenusLoc.Root + "/events/DecoratedHeroesShop";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string ChallengesTabBtn = Root + "/bg/submenuButtons/decoratedHeroesChallenges";

        public const string ExchangeTabBtn = Root + "/bg/submenuButtons/decoratedHeroesExchange";

        // 8 "challenge (N)" cards confirmed via UnityPy dump and the user's own screenshot, each
        // independently claimable regardless of the others - no priority/comparison needed, just
        // claim every one that's currently clickable.
        public const string ChallengeGridRoot =
            Root + "/bg/submenus/decoratedHeroesChallenges/challengesLayout";

        // Relative to a challenge card GameElement (a "challenge (N)" child of ChallengeGridRoot).
        public const string ChallengeClaimBtn = "/claimButton";

        // 16 "item (N)" slots confirmed via UnityPy dump - a fully-instantiated list (not a pooled/
        // recycled ScrollView like Collector Quest's chests), so index N always means the same
        // catalog item; matched by name instead of index anyway (see DecoratedHeroesShop.BuyItem) so
        // this doesn't depend on that assumption holding.
        public const string ExchangeItemsRoot =
            Root + "/bg/submenus/decoratedHeroesExchange/scrollView/viewport/content";

        // Cycling bulk-quantity multiplier, same shape as ChaosRift.ChangeHitQuantityBtn - per the
        // user's standing instruction (2026-09-23) to always use a multiplier when one exists. Exact
        // cycle values not live-confirmed yet - see DecoratedHeroesShop.TrySetBestQuantity.
        public const string ExchangeQuantityBtn =
            Root + "/bg/submenus/decoratedHeroesExchange/changeQuantityButton";

        // Relative to an exchange item GameElement (an "item (N)" child of ExchangeItemsRoot).
        public const string ExchangeItemNameTxt = "/itemName";

        public const string ExchangeItemBuyBtn = "/purchaseButton";

        public const string ExchangeQuantityTxt = ExchangeQuantityBtn + "/text";
    }
}
