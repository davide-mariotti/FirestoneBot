namespace Firebot.Infrastructure;

/// <summary>
///     The "bag" screen (4 tabs: items, scrolls, chests, currencies) and the chest-opening popup
///     flow. No prior precedent at all. Entry button confirmed by the user (battle screen "bag" icon) -
///     same bottomSideUIDesktop HUD region as Path of Glory's button, see that comment in Battle.cs.
/// </summary>
public static partial class Paths
{
    public static class InventoryLoc
    {
        private const string Root = MenusLoc.Root + "/menus/Inventory";

        public const string CloseBtn = Root + "/closeButton";

        public const string ChestsTabBtn = Root + "/submenuButtons/chests";

        public const string ItemsTabBtn = Root + "/submenuButtons/inventoryItems";

        // Live-confirmed, 2026-09-17: "submenus" has exactly one child, "items" - the 4 tabs (see
        // class doc above) share this single content pane rather than each having their own
        // "submenus/<tabName>/..." tree (an earlier attempt to "fix" this to "submenus/chests/..."
        // was wrong and reverted - that path doesn't exist at all). This path itself was fine all
        // along; the real bug was a timing race, see CollectorQuestTask.
        public const string ContentRoot = Root + "/submenus/items/ScrollView/Viewport/Content";

        // Live-confirmed, 2026-09-17: the real slot name is "Common" (capitalized, no "chestbox"
        // suffix) - the previously-assumed "commonChestbox" never existed at all. Siblings found
        // the same way: "Uncommon", "Rare", "Epic" (not individually wired up - the generic scan in
        // CollectorQuestTask picks up any of these by not being in KnownNonChestSlots).
        public const string CommonChestSlot = "/Common";

        // Confirmed non-chest slot names sharing this same Content list - excluded when scanning for
        // "any other openable chest", since these aren't chests at all (mystery box/gift claims,
        // stat-boost consumables). jewelChest/celestialChest were excluded here too until the user
        // asked to open those as well (they pile up unopened from Pharaoh's Vault rewards) - simplest
        // to fold into Collector's existing generic scan rather than a separate task, at the cost of
        // Collector now opening more than just the "Collector" quest's gear chests.
        public static readonly string[] KnownNonChestSlots =
        {
            "mysteryBox", "oraclesGift", "midasTouch", "StrangeDust", "Speed"
        };
    }

    // Opened by clicking a chest slot in InventoryLoc.ContentRoot. Found via a fresh UnityPy scan -
    // not in the docs, and no prior precedent (chest opening was never automated before).
    public static class ChestOpenPreviewLoc
    {
        private const string Root = MenusLoc.Root + "/popups/ChestOpenPreview";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string OpenX1Btn = Root + "/bg/openingOptions/openx1";

        public const string OpenX10Btn = Root + "/bg/openingOptions/openx10";
    }

    // Results screen shown after a batch open - has its OWN copy of the same openingOptions buttons,
    // letting you chain more batch-opens of the same chest type without navigating back.
    public static class ChestOpeningLoc
    {
        private const string Root = MenusLoc.Root + "/popups/ChestOpening";

        public const string CloseBtn = Root + "/closeButton";

        public const string OpenX1Btn = Root + "/openingOptions/openx1";

        public const string OpenX10Btn = Root + "/openingOptions/openx10";
    }
}
