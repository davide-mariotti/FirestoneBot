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

        // '.'-grouped past 999 like the other counters ("336" on Steam-0, 30/09).
        public const string CoinCountTxt = Root + "/counters/currencyInteraction (ExoticCoin)/quantity";

        public static class SellLoc
        {
            public const string ProductGridRoot =
                ExoticMerchantLoc.Root + "/submenus/bg/sellItemsSubmenu/Scroll View/Viewport/Content/productGrid";

            // Relative to a product in ProductGridRoot, for Transform.Find: from the fourth product
            // on the cells are all "exoticMerchantSellItem(Clone)", so they're walked by index.
            public const string ItemNameTxt = "itemName";

            public const string ItemQuantityTxt = "itemBg/quantity";

            // Sells one item per click.
            public const string SellBtn = "sellButton";
        }

        public static class UpgradesLoc
        {
            private const string SubmenuRoot = ExoticMerchantLoc.Root + "/submenus/bg/upgradesSubmenu";

            public const string UpgradesListRoot =
                SubmenuRoot + "/upgradesScrollView/Viewport/Content/upgradesList";

            // Relative to an exoticMerchantUpgrade (N) child of UpgradesListRoot.
            public const string UpgradeBtn = "/upgradeButton";

            // Seen live (30/09): sprite exoticCoin64 on all 12 upgrades, next to costText (960-2208).
            public const string UpgradeCostIcon = UpgradeBtn + "/currencyIcon";

            public const string UpgradeCostTxt = UpgradeBtn + "/costText";
        }

        // All seen live on Steam-0 (level 166, 01/10). The tab's lock is inactive once unlocked.
        public static class EmblemMarketLoc
        {
            private const string SubmenuRoot = ExoticMerchantLoc.Root + "/submenus/bg/emblemMarketSubmenu";

            public const string TabBtn = ExoticMerchantLoc.Root + "/submenus/submenuButtons/emblemMarket";

            public const string TabLock = TabBtn + "/lock";

            public const string GearCategoryBtn = SubmenuRoot + "/categoryButtons/gear";

            public const string JewelsCategoryBtn = SubmenuRoot + "/categoryButtons/jewels";

            // Cells "exoticMerchantEmblemGearChest (0..3)" / "exoticMerchantEmblemJewelChest (0..3)",
            // each carrying the game's chest component (ExoticMerchant.EmblemOffers).
            public const string GearGrid = SubmenuRoot + "/categories/gearCategory/scrollView/Viewport/Content/productGrid";

            public const string JewelsGrid = SubmenuRoot + "/categories/jewelsCategory/scrollView/Viewport/Content/productGrid";

            // Each shown only while its category is open, '.'-grouped ("8.878", "10.100").
            public const string CourageCountTxt = ExoticMerchantLoc.Root + "/counters/currencyInteraction (EmblemOfCourage)/quantity";

            public const string ValorCountTxt = ExoticMerchantLoc.Root + "/counters/currencyInteraction (EmblemOfValor)/quantity";
        }
    }
}
