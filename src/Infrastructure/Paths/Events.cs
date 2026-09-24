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

        // Live-confirmed, 2026-09-24 (Steam-12/13/14/15): an "active" event card can still be
        // individually level/time-locked for a young account (structure dump showed mainElements/lock,
        // fadeLevelLocked/locked and fadeTimeLocked/timer all active) - clicking it never opens
        // anything, which is correct/expected, not a click bug. Checked before attempting a click at
        // all, so a locked card is skipped immediately instead of wasting retries on it every run.
        public const string CardLockIndicator = "/mainElements/lock";
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

    /// <summary>
    ///     "New Player Event"'s real screen - the game recycles an old "AnniversaryShop" prefab/script
    ///     name for it rather than a purpose-built one (live-confirmed, 2026-09-24, Steam-10, via
    ///     Watchdog.DumpActiveScreens right after clicking the "New Player Event" card in
    ///     EventManager - not a guess, the actual live root name). 6 tabs total
    ///     (Dailies/Activity/Exchange/Avatars/Skins/Shop); per the user, only Dailies (check-in),
    ///     Activity (online-time milestones) and Exchange (buy "Rare chest") are wired here. Avatars,
    ///     Skins and the Shop tab (real-money packs + a paid "anniversaryPass", confirmed via a live
    ///     dump: its 7 "anniversaryProduct" packs were all inactive on this account, plausibly IAP
    ///     disabled on this platform/build) are deliberately never touched, same convention as
    ///     DecoratedHeroesShopLoc's own excluded tabs.
    /// </summary>
    public static class AnniversaryShopLoc
    {
        public const string Root = MenusLoc.Root + "/events/AnniversaryShop";

        public const string CloseBtn = Root + "/bg/closeButton";

        private const string TabsRoot = Root + "/bg/submenuButtons";

        public const string DailiesTabBtn = TabsRoot + "/anniversaryDailies";

        public const string ActivityTabBtn = TabsRoot + "/anniversaryActivity";

        public const string ExchangeTabBtn = TabsRoot + "/anniversaryExchange";

        // Dailies tab: a single check-in button for the current day (same shape as
        // StoreLoc.DailyRewardsLoc.CheckInBtn) - the per-day grid cells (rewards/grid/
        // anniversaryDailyInteraction (0)..(13), 14 days) only show state (canClaim/claimedFrame),
        // confirmed via a live structure dump with no claim button of their own.
        public static class DailiesLoc
        {
            private const string Root = AnniversaryShopLoc.Root + "/bg/submenus/anniversaryDailies";

            public const string CheckInBtn = Root + "/banner/checkinButton";
        }

        // Activity tab: per the user (2026-09-24), earned by staying online a certain amount of time
        // that day. Live-confirmed structure (full recursive dump, 2026-09-24, Steam-10): milestoneLayout/
        // milestone (N)/locked|unlocked|dayText - the real claim button is nested at
        // unlocked/claimButton, NOT on the milestone card itself (that first guess was live-disproven:
        // "Component <Button> missing" on the card). claimButton is only active once that milestone's
        // reward is actually ready to collect (inactive otherwise, including after being claimed).
        public static class ActivityLoc
        {
            public const string MilestonesRoot =
                AnniversaryShopLoc.Root + "/bg/submenus/anniversaryActivity/spriteMask/scrollView/viewport/milestoneLayout";

            // Relative to a milestone child - active means still locked.
            public const string LockedIndicator = "/locked";

            // Relative to a milestone child.
            public const string ClaimBtn = "/unlocked/claimButton";
        }

        // Exchange tab: spends "ActivityCoin" (the event's own currency, confirmed via
        // counterInteractionEventAnniversary). Live-confirmed real items, 2026-09-24: Rare chest
        // (3.800, the user's target - "first position" in this list), Golden chest (3.800), Exotic
        // coin (1.900), Pickaxe (550), Strange dust (750), Honor (2.300), Beer (1.900), Meteorite
        // (2.300), Golden key (4.500), Cobra key (4.500).
        public static class ExchangeLoc
        {
            private const string Root = AnniversaryShopLoc.Root + "/bg/submenus/anniversaryExchange";

            public const string ChangeQuantityBtn = Root + "/changeQuantityButton";

            public const string ChangeQuantityTxt = ChangeQuantityBtn + "/text";

            public const string ItemsRoot = Root + "/scrollView/viewport/content";

            // Relative to an item GameElement (an "item (N)" child of ItemsRoot).
            public const string ItemNameTxt = "/itemName";

            public const string ItemBuyBtn = "/purchaseButton";
        }
    }

    /// <summary>
    ///     "Mass Production"'s real screen - the game recycles an old "MiniEvents" prefab/script name
    ///     for it (live-confirmed, 2026-09-24, Steam-2, via Watchdog.DumpActiveScreens right after
    ///     clicking the "Mass Production" card in EventManager). Two tabs: "offers" (default, real-
    ///     money packs - deliberately never touched, same convention as every other event shop's
    ///     excluded real-money tab) and "challenges" (a day-by-day challenge track - per the user,
    ///     2026-09-24, claim every currently-available one).
    /// </summary>
    public static class MiniEventsLoc
    {
        public const string Root = MenusLoc.Root + "/events/MiniEvents";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string ChallengesTabBtn = Root + "/bg/submenuButtons/challenges";

        // Live-confirmed structure, 2026-09-24: miniEventChallengeInteraction (N), one per day, unlocked
        // day-by-day (later days show "locked" active instead of "unlocked") - same
        // "whole grid, click whatever's currently clickable" idiom as DecoratedHeroesShopLoc's own
        // ChallengeGridRoot.
        public const string ChallengesGridRoot =
            Root + "/bg/submenus/challenges/bg/challengesLayout";

        // Relative to a challenge card GameElement (a "miniEventChallengeInteraction (N)" child of
        // ChallengesGridRoot) - nested under "unlocked" (only present/active once that day's challenge
        // is unlocked), unlike DecoratedHeroesShopLoc's claim button which sits directly on the card.
        public const string ChallengeClaimBtn = "/unlocked/reward/claimButton";
    }
}
