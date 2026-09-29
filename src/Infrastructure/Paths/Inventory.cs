namespace Firebot.Infrastructure;

/// <summary>The bag screen and the chest-opening popups.</summary>
public static partial class Paths
{
    public static class InventoryLoc
    {
        private const string Root = MenusLoc.Root + "/menus/Inventory";

        public const string CloseBtn = Root + "/closeButton";

        public const string ChestsTabBtn = Root + "/submenuButtons/chests";

        // All four tabs share this one content pane; there is no submenus/<tab>/ tree.
        public const string ContentRoot = Root + "/submenus/items/ScrollView/Viewport/Content";

        // Gear chest slots relative to ContentRoot, by rarity name, all seen live (29/09). The rarer
        // ones are only opened when the Collector quest has nothing cheaper left.
        public const string WoodenChestSlot = "/Wooden";

        public const string IronChestSlot = "/Iron";

        public const string CommonChestSlot = "/Common";

        public const string UncommonChestSlot = "/Uncommon";

        public const string RareChestSlot = "/Rare";

        public const string EpicChestSlot = "/Epic";

        public const string LegendaryChestSlot = "/Legendary";
    }

    // Opened by clicking a chest slot.
    public static class ChestOpenPreviewLoc
    {
        private const string Root = MenusLoc.Root + "/popups/ChestOpenPreview";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string OpenX1Btn = Root + "/bg/openingOptions/openx1";

        public const string OpenX10Btn = Root + "/bg/openingOptions/openx10";
    }

    // The results screen after an open, with its own copy of the open buttons to chain more.
    public static class ChestOpeningLoc
    {
        private const string Root = MenusLoc.Root + "/popups/ChestOpening";

        public const string CloseBtn = Root + "/closeButton";

        public const string OpenX1Btn = Root + "/openingOptions/openx1";

        public const string OpenX10Btn = Root + "/openingOptions/openx10";
    }
}
