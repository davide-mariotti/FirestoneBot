namespace Firebot.Infrastructure;

/// <summary>
///     War machine roster/leveling screen (https://firestone-idle-rpg.fandom.com/wiki/War_Machines).
///     Live-confirmed 2026-09-18: NOT reached through the Engineer screen (the "warMachinesButton"
///     once assumed there never existed) - the Engineer building instead opens a "GarageSelection"
///     choice popup, and this screen lives behind its sibling "garage" card - see
///     Town.OpenWarMachines.
/// </summary>
public static partial class Paths
{
    public static class WarMachinesLoc
    {
        private const string Root = MenusLoc.Root + "/menus/WarMachines";

        public const string CloseBtn = Root + "/closeButton";

        // Pooled scroll list - children are "warMachineSquare (0)", "(1)", ... plus
        // "nextWarMachineUnlock" and "allWarMachinesButton" (not real machines), filtered by name.
        public const string MachineGridRoot = Root + "/warMachinesScrollView/Viewport/grid";

        // 4 tabs confirmed via UnityPy: garage/workshop/blueprintUpgrades/rarityUpgrades. Only
        // "workshop" (the actual level-up action) is wired here - "garage" is a read-only stat sheet
        // with no button at all; blueprintUpgrades/rarityUpgrades are separate wiki mechanics
        // (per-attribute currency spend, rarity tiers) not covered by this task.
        public const string WorkshopTabBtn = Root + "/submenus/submenuButtons/workshop";

        private const string WorkshopRoot = Root + "/submenus/bg/workshopSubmenu";

        // Costs Expedition Tokens + 3 components (Screw/Cog/Metal per the wiki) for +100 xp - both
        // enforced by the button's own clickable state, no thresholds hardcoded here, same as every
        // other gated action in this codebase.
        public const string LevelUpBtn = WorkshopRoot + "/upgradeButton";

        // A single cycling bulk-quantity multiplier (same shape as ScarabGame/PharaohsVault's
        // changeBetQuantity, not Awakening's 8 distinct buttons) - not wired yet: the max-value label
        // format isn't confirmed live for this screen, and cycling a shared button without a verified
        // stop condition risks landing on an arbitrary value instead of the max. Repeated LevelUpBtn
        // clicks reach the same end state regardless, just in more steps - this is a possible future
        // click-count optimization, not a functional gap.
        public const string ChangeQuantityBtn = WorkshopRoot + "/changeQuantityButton";
    }
}
