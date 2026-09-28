namespace Firebot.Infrastructure;

/// <summary>The Character screen's Talents tab: talentInteraction (0)..(88) in tier order.</summary>
public static partial class Paths
{
    public static class TalentsLoc
    {
        public const string Root = MenusLoc.CharacterLoc.Root + "/bg/submenus/talents";

        public const string NodeRoot = Root + "/bg/talentTreeScroll/Viewport/content/talentTree";

        // "available/total", e.g. "1/96".
        public const string PointsLeftTxt = Root + "/bg/talentPoints/container/pointInfo/talentsTextBg/talentsText";

        // Upgrading only stages a point; this commits it. The resetTalentsButton near the points
        // counter resets the whole tree for 100 gems - never click it.
        public const string SaveBtn = Root + "/bg/talentsSaveButton";
    }

    // Opened by clicking a talent node.
    public static class TalentPreviewLoc
    {
        public const string Root = MenusLoc.Root + "/popups/TalentPreview";

        public const string CloseBtn = Root + "/bg/closeButton";

        // "Level N/M", e.g. "Level 1/25".
        public const string LevelTxt = Root + "/bg/talentLevelText";

        // Shown instead of the upgrade buttons while this node's prerequisites aren't met.
        public const string LockedRoot = Root + "/bg/talentLocked";

        public const string UpgradeBtn = Root + "/bg/talentUpgrades/upgradeTalentButton";
    }
}
