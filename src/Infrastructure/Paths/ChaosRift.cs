namespace Firebot.Infrastructure;

/// <summary>
///     Chaos Rift: a server-wide boss attacked with Moon Stones (free, 10/day). Damage earns Dark
///     Rune, spent in the supplies shop.
/// </summary>
public static partial class Paths
{
    public static class ChaosRiftLoc
    {
        public const string Root = MenusLoc.Root + "/menus/ChaosRift";

        public const string CloseBtn = Root + "/closeButton";

        // Root's other children are background layers; the interactive layer is nested here.
        private const string UiRoot = Root + "/UIElements";

        public const string HitBtn = UiRoot + "/godParent/hitButton";

        public const string AutoHitToggleBtn = UiRoot + "/autoHitToggle";

        public const string ChangeHitQuantityBtn = UiRoot + "/changeHitQuantity";

        public const string HitQuantityTxt = ChangeHitQuantityBtn + "/text";

        public const string ShopBtn = UiRoot + "/actionButtons/shop";

        // Opens Magic Quarters on its Chaos Rift (holy damage) tab.
        public const string UpgradesBtn = UiRoot + "/actionButtons/upgrades";
    }

    public static class ChaosRiftShopLoc
    {
        // Only comes into existence after the first "shop" click - until then every path under it
        // reads as broken, not just hidden.
        public const string Root = MenusLoc.Root + "/menus/ChaosRiftShop";

        public const string CloseBtn = Root + "/closeButton";

        // The only tab paid in Dark Rune. Sale, Daily Deals and Weekly Deals cost real money.
        public const string SuppliesTabBtn = Root + "/bg/submenuButtons/supplies/button";

        private const string SuppliesRoot = Root + "/bg/submenus/supplies/items";

        // Tomes of Power only. The moonstoneOne / moonstoneBulk items beside it are Eclipse Stones,
        // which the user never wants bought.
        public const string TomeOfPowerBuyBtn = SuppliesRoot + "/tomeOfPower/claimBg/purchaseButton";
    }
}
