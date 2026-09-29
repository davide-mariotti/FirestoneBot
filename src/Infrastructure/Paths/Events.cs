namespace Firebot.Infrastructure;

/// <summary>
///     The Events hub and each event's shop. They live under "events/", not "menus/". Event shops
///     reuse old prefab names: New Player Event is "AnniversaryShop", Mass Production and Sigils of Prophecy
///     are "MiniEvents".
///     Only claims and exchange purchases are wired - never the real-money tabs (Market, Shop, Offers).
/// </summary>
public static partial class Paths
{
    public static class EventManagerLoc
    {
        public const string Root = MenusLoc.Root + "/events/EventManager";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string ActiveEventsRoot =
            Root + "/bg/verticalLayout/Scroll View/Viewport/Content/activeEvents";

        // "upcomming" is the game's own spelling - don't correct it.
        public const string UpcomingEventsRoot =
            Root + "/bg/verticalLayout/Scroll View/Viewport/Content/upcommingEvents";

        // Relative to an event card (an "eventInteraction" child of the two roots above).
        public const string CardTitleTxt = "/mainElements/title";

        // Shown on cards still level- or time-locked for this account; clicking those opens nothing.
        public const string CardLockIndicator = "/mainElements/lock";
    }

    public static class DecoratedHeroesShopLoc
    {
        public const string Root = MenusLoc.Root + "/events/DecoratedHeroesShop";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string ChallengesTabBtn = Root + "/bg/submenuButtons/decoratedHeroesChallenges";

        public const string ExchangeTabBtn = Root + "/bg/submenuButtons/decoratedHeroesExchange";

        public const string ChallengeGridRoot =
            Root + "/bg/submenus/decoratedHeroesChallenges/challengesLayout";

        // Relative to a "challenge (N)" card.
        public const string ChallengeClaimBtn = "/claimButton";

        public const string ExchangeItemsRoot =
            Root + "/bg/submenus/decoratedHeroesExchange/scrollView/viewport/content";

        public const string ExchangeQuantityBtn =
            Root + "/bg/submenus/decoratedHeroesExchange/changeQuantityButton";

        // Relative to an "item (N)" in ExchangeItemsRoot.
        public const string ExchangeItemNameTxt = "/itemName";

        public const string ExchangeItemBuyBtn = "/purchaseButton";

        public const string ExchangeQuantityTxt = ExchangeQuantityBtn + "/text";
    }

    /// <summary>New Player Event.</summary>
    public static class AnniversaryShopLoc
    {
        public const string Root = MenusLoc.Root + "/events/AnniversaryShop";

        public const string CloseBtn = Root + "/bg/closeButton";

        private const string TabsRoot = Root + "/bg/submenuButtons";

        public const string DailiesTabBtn = TabsRoot + "/anniversaryDailies";

        public const string ActivityTabBtn = TabsRoot + "/anniversaryActivity";

        public const string ExchangeTabBtn = TabsRoot + "/anniversaryExchange";

        public static class DailiesLoc
        {
            private const string Root = AnniversaryShopLoc.Root + "/bg/submenus/anniversaryDailies";

            public const string CheckInBtn = Root + "/banner/checkinButton";
        }

        // Milestones earned by time spent online that day.
        public static class ActivityLoc
        {
            public const string MilestonesRoot =
                AnniversaryShopLoc.Root + "/bg/submenus/anniversaryActivity/spriteMask/scrollView/viewport/milestoneLayout";

            // Relative to a milestone Transform, for Transform.Find (no leading slash). Active only
            // while that milestone's reward is ready to collect.
            public const string MilestoneClaimBtn = "unlocked/claimButton";
        }

        public static class ExchangeLoc
        {
            private const string Root = AnniversaryShopLoc.Root + "/bg/submenus/anniversaryExchange";

            public const string ChangeQuantityBtn = Root + "/changeQuantityButton";

            public const string ChangeQuantityTxt = ChangeQuantityBtn + "/text";

            public const string ItemsRoot = Root + "/scrollView/viewport/content";

            // Relative to an "item (N)" in ItemsRoot.
            public const string ItemNameTxt = "/itemName";

            public const string ItemBuyBtn = "/purchaseButton";
        }
    }

    /// <summary>Mass Production and Sigils of Prophecy. Its default tab ("offers") is real-money packs.</summary>
    public static class MiniEventsLoc
    {
        public const string Root = MenusLoc.Root + "/events/MiniEvents";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string ChallengesTabBtn = Root + "/bg/submenuButtons/challenges";

        // One miniEventChallengeInteraction (N) per day, unlocked day by day.
        public const string ChallengesGridRoot =
            Root + "/bg/submenus/challenges/bg/challengesLayout";

        // Relative to a day card; exists only once that day is unlocked.
        public const string ChallengeClaimBtn = "/unlocked/reward/claimButton";
    }
}
