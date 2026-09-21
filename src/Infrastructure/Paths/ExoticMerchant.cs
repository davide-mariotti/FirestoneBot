namespace Firebot.Infrastructure;

/// <summary>Exotic Merchant: sell scrolls/inventory items for exotic coins, then spend them on
/// upgrades. Never automated before.</summary>
public static partial class Paths
{
    public static class ExoticMerchantLoc
    {
        private const string Root = MenusLoc.Root + "/menus/ExoticMerchant";

        public const string CloseBtn = Root + "/closeButton";

        public const string SellItemsTabBtn = Root + "/submenus/submenuButtons/sellItems";

        public const string UpgradesTabBtn = Root + "/submenus/submenuButtons/upgrades";

        public static class SellLoc
        {
            public const string ProductGridRoot =
                ExoticMerchantLoc.Root + "/submenus/bg/sellItemsSubmenu/Scroll View/Viewport/Content/productGrid";

            // Relative to a product grid child - single fixed action (no quantity choice), matches
            // "always sell x1" since that's the only option this button offers.
            public const string SellBtn = "/sellButton";
        }

        public static class UpgradesLoc
        {
            private const string SubmenuRoot = ExoticMerchantLoc.Root + "/submenus/bg/upgradesSubmenu";

            public const string NextTreeBtn = SubmenuRoot + "/navigation/goForthTree";

            public const string PreviousTreeBtn = SubmenuRoot + "/navigation/goBackTree";

            public const string UpgradesListRoot =
                SubmenuRoot + "/upgradesScrollView/Viewport/Content/upgradesList";

            // Relative to an exoticMerchantUpgrade (N) child - inline button, no separate preview
            // popup (unlike Firestone/Meteorite Research), confirmed via UnityPy.
            public const string UpgradeBtn = "/upgradeButton";
        }
    }
}
