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

        // Inactive in the prefab (UnityPy dump, 03/10); assumed to replace the open buttons on a chest
        // the account can't open yet (/Iron never opened on the level 30-59 accounts).
        public const string RequirementText = Root + "/bg/chestRequirementBg/text";
    }

    // "New items" (a jewel never owned before), raised over the results screen by Golden chests;
    // closed live by ChestOpening on Steam-1..11 (01/10).
    public static class NewItemLoc
    {
        private const string Root = MenusLoc.Root + "/popups/NewItem";

        public const string CloseBtn = Root + "/bg/closeButton";
    }

    // The results screen after an open, with its own copy of the open buttons to chain more. A menu,
    // not a popup: under popups/ the chaining never found its buttons and each slot opened one chest
    // (all 17 instances, 30/09; the Watchdog closed it as menus/ChestOpening).
    public static class ChestOpeningLoc
    {
        private const string Root = MenusLoc.Root + "/menus/ChestOpening";

        public const string CloseBtn = Root + "/closeButton";

        public const string OpenX1Btn = Root + "/openingOptions/openx1";

        public const string OpenX10Btn = Root + "/openingOptions/openx10";
    }
}
