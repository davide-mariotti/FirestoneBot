namespace Firebot.Infrastructure;

/// <summary>Tree of Life. Only the Personal tree (paid in Expedition Tokens) is wired, never the Guild one.</summary>
public static partial class Paths
{
    public static class TreeOfLifeLoc
    {
        private const string Root = MenusLoc.Root + "/menus/TreeOfLife";

        public const string CloseBtn = Root + "/closeButton";

        public const string PersonalTabBtn = Root + "/submenuButtons/personalTree";

        // treeOfLifePersonalUpgrade (0)..(19); each carries its game data (ToLPersonalUpgradeInteraction).
        public const string PersonalNodeRoot = Root + "/submenus/personalTree/talentTree";

        // Clicking a node only opens this preview; the purchase happens in it.
        private const string PersonalUpgradePreviewRoot = MenusLoc.Root + "/popups/TOLPersonalUpgradePreview";

        public const string PersonalUpgradePreviewCloseBtn = PersonalUpgradePreviewRoot + "/bg/closeButton";

        // Exists only under "bg/normal" - a maxed node shows "bg/maxed", which has no buy button.
        public const string PersonalUpgradePreviewBuyBtn = PersonalUpgradePreviewRoot + "/bg/normal/buyUpgradeButton";
    }
}
