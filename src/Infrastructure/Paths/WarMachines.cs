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
    }
}
