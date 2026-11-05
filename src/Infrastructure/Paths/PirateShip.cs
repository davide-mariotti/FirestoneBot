namespace Firebot.Infrastructure;

/// <summary>
///     The Pirate Ship building. Only the Pirate's Prize free track is automated - Mercenaries,
///     Captain's Deal and Skins all spend currency.
/// </summary>
public static partial class Paths
{
    public static class PirateShipLoc
    {
        private const string Root = MenusLoc.Root + "/menus/PirateShip";

        public const string CloseBtn = Root + "/closeButton";

        // Pirate's Prize is already the open tab when the screen opens. Don't click its tab button:
        // that resets the reward list's scroll position.
        public static class PiratesPrizeLoc
        {
            private const string SubmenuRoot = Root + "/bg/submenus/piratesPrize";

            // ~20 tiers that ALL share the name "ppTierInteraction(Clone)", so they can't be told
            // apart by path - PiratesPrizeTask walks this root's children as raw Transforms.
            public const string TierListRoot = SubmenuRoot + "/Scroll View/Viewport/content/rewardsTierSection";

            // The rewards are split into pages, shown one at a time. A bell on an arrow means rewards
            // left unclaimed that way, and those keep the rail badge lit.
            public const string LeftArrowBtn = SubmenuRoot + "/navigator/leftArrow";

            public const string LeftArrowBell = LeftArrowBtn + "/notification";

            public const string RightArrowBtn = SubmenuRoot + "/navigator/rightArrow";

            public const string RightArrowBell = RightArrowBtn + "/notification";

            // Relative to a tier Transform, for Transform.Find (no leading slash). Only the free
            // track: piratesPrizePaidInteraction and every purchaseButton/purchasePrice cost currency.
            public const string TierFreeClaimBtn = "piratesPrizeFreeInteraction/purchaseFreeButton";
        }
    }
}
