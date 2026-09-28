namespace Firebot.Infrastructure;

/// <summary>Tree of Life. Only the Personal tree (paid in Expedition Tokens) is wired, never the Guild one.</summary>
public static partial class Paths
{
    public static class TreeOfLifeLoc
    {
        private const string Root = MenusLoc.Root + "/menus/TreeOfLife";

        public const string CloseBtn = Root + "/closeButton";

        public const string PersonalTabBtn = Root + "/submenuButtons/personalTree";

        // treeOfLifePersonalUpgrade (0)..(19), in the wiki table's order.
        public const string PersonalNodeRoot = Root + "/submenus/personalTree/talentTree";

        // Relative to a node: its current level, shown right on the grid.
        public const string NodeLevelTxt = "/levelBg/level";

        // Clicking a node only opens this preview; the purchase happens in it.
        private const string PersonalUpgradePreviewRoot = MenusLoc.Root + "/popups/TOLPersonalUpgradePreview";

        public const string PersonalUpgradePreviewCloseBtn = PersonalUpgradePreviewRoot + "/bg/closeButton";

        // Exists only under "bg/normal" - a maxed node shows "bg/maxed", which has no buy button.
        public const string PersonalUpgradePreviewBuyBtn = PersonalUpgradePreviewRoot + "/bg/normal/buyUpgradeButton";
    }
}
