namespace Firebot.Infrastructure;

/// <summary>Exotic Merchant: sells items for Exotic Coins, which buy Exotic Upgrades.</summary>
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

            // Relative to a product in ProductGridRoot; sells one item per click.
            public const string SellBtn = "/sellButton";
        }

        public static class UpgradesLoc
        {
            private const string SubmenuRoot = ExoticMerchantLoc.Root + "/submenus/bg/upgradesSubmenu";

            public const string UpgradesListRoot =
                SubmenuRoot + "/upgradesScrollView/Viewport/Content/upgradesList";

            // Relative to an exoticMerchantUpgrade (N) child of UpgradesListRoot.
            public const string UpgradeBtn = "/upgradeButton";
        }
    }
}
