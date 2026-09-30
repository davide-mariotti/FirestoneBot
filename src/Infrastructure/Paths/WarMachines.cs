namespace Firebot.Infrastructure;

/// <summary>War machine leveling, reached through the Engineer building's "garage" card.</summary>
public static partial class Paths
{
    public static class WarMachinesLoc
    {
        private const string Root = MenusLoc.Root + "/menus/WarMachines";

        public const string CloseBtn = Root + "/closeButton";

        // "warMachineSquare (N)" children plus two that aren't machines (nextWarMachineUnlock, allWarMachinesButton).
        public const string MachineGridRoot = Root + "/warMachinesScrollView/Viewport/grid";

        public const string WorkshopTabBtn = Root + "/submenus/submenuButtons/workshop";

        private const string WorkshopRoot = Root + "/submenus/bg/workshopSubmenu";

        // Costs Expedition Tokens plus components; the button's own clickable state reflects both.
        public const string LevelUpBtn = WorkshopRoot + "/upgradeButton";

        public const string RarityTabBtn = Root + "/submenus/submenuButtons/rarityUpgrades";

        // Seen live on Steam-0 (30/09): "Increase rarity", 51.000 Tools (icon toolsIcon64) for
        // Goliath Uncommon -> Rare, not interactable with 7.476 Tools.
        public const string RarityBtn = Root + "/submenus/bg/rarityUpgradesSubmenu/rarityButton";

        public const string RarityCostIcon = RarityBtn + "/currencyIcon";

        public const string RarityCostTxt = RarityBtn + "/costText";

        public const string ToolsTxt = Root + "/counters/currencyInteraction (Tools)/quantity";
    }
}
