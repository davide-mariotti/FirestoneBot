namespace Firebot.Infrastructure;

/// <summary>
///     Tree of Life - two separate trees (Personal, kept even if you leave your guild; Guild, shared
///     with guildmates), both bought with a currency (the personal one with Expedition Tokens per the
///     wiki). Only the Personal tab is wired here, per the user. Reached from Guild -&gt;
///     TownGuildLoc.TreeOfLifeBtn. This whole feature is new, found via a targeted
///     UnityPy scan requested by the user.
/// </summary>
public static partial class Paths
{
    public static class TreeOfLifeLoc
    {
        private const string Root = MenusLoc.Root + "/menus/TreeOfLife";

        public const string CloseBtn = Root + "/closeButton";

        public const string PersonalTabBtn = Root + "/submenuButtons/personalTree";

        // Index 0-19, confirmed via UnityPy: the wiki's Personal Tree table lists exactly 20
        // upgrades, matching treeOfLifePersonalUpgrade (0)-(19) found live 1:1 (same validation
        // technique already used for the Talents tree - see GameModel/Features/Guild/TreeOfLife.cs
        // for the name catalog built from that table's row order).
        public const string PersonalNodeRoot = Root + "/submenus/personalTree/talentTree";

        // Relative to a node - shows its current level directly on the grid. Still used as the
        // affordability/cost tie-break even though a preview popup DOES exist (see below) - the
        // wiki confirms cost scales purely with an upgrade's own current level.
        public const string NodeLevelTxt = "/levelBg/level";

        // Live-confirmed, 2026-09-18: clicking a personal tree node opens this popup ("Magic spells /
        // Level 0/5 / Buy upgrade 600") rather than buying directly - the original NodeLevelTxt
        // assumption ("no preview popup") was wrong, found via a generic active-screen dump after
        // TreeOfLifeTask got stuck re-clicking the same node for a full 120s (it kept re-opening this
        // same popup since nothing here ever clicked its own buy/close buttons).
        private const string PersonalUpgradePreviewRoot = MenusLoc.Root + "/popups/TOLPersonalUpgradePreview";

        public const string PersonalUpgradePreviewCloseBtn = PersonalUpgradePreviewRoot + "/bg/closeButton";

        // Live-confirmed, 2026-09-18: only present/visible under "bg/normal" (the not-yet-maxed
        // state) - "bg/maxed" has no buy button at all, matching a fully maxed upgrade.
        public const string PersonalUpgradePreviewBuyBtn = PersonalUpgradePreviewRoot + "/bg/normal/buyUpgradeButton";
    }
}
