namespace Firebot.Infrastructure;

/// <summary>
///     Character menu's Talents tab (Paths.MenusLoc.CharacterLoc.TalentsTabBtn) - 89 individually
///     addressable nodes across the game's 46 talent tiers, confirmed via a full UnityPy scan: summing
///     each tier's node count from the wiki's Talent Tree table gives exactly 89, matching
///     talentInteraction (0)-(88) found live 1:1 in tier order. See
///     GameModel/Features/Character/Talents.cs for the full catalog and the priority order this task
///     follows. This whole feature is new.
/// </summary>
public static partial class Paths
{
    public static class TalentsLoc
    {
        // Live-confirmed, 2026-09-18: the Character screen is a popup (MenusLoc.CharacterLoc.Root =
        // "popups/Character"), not "menus/Character" - this was defined independently and guessed
        // wrong instead of reusing that already-confirmed root (same wrong-guess pattern as
        // BattlePass/TavernMarket/PharaohsVault/ScarabGameShop earlier the same day). Everything below
        // came back "path broken" as a result, including the root itself.
        public const string Root = MenusLoc.CharacterLoc.Root + "/bg/submenus/talents";

        // Index 0-88, tier order matching the wiki's Talent Tree table exactly (see the validation
        // above). Each is a small icon+level button that opens TalentPreviewLoc.
        public const string NodeRoot = Root + "/bg/talentTreeScroll/Viewport/content/talentTree";

        public const string PointsLeftTxt = Root + "/bg/talentPoints/container/pointInfo/talentsTextBg/talentsText";

        // Confirmed by the user: upgradeTalentButton only stages a point, this actually commits it.
        public const string SaveBtn = Root + "/bg/talentsSaveButton";

        // NEVER click: resets the ENTIRE talent tree for 100 gems (confirmed via the wiki), refunding
        // every point spent so it can all be reallocated - not a per-node action, and real premium
        // currency. Deliberately unused by any task.
        public const string ResetTreeBtnDoNotUse =
            Root + "/bg/talentPoints/container/pointInfo/resetTalentsButton";
    }

    // Opened by clicking a TalentsLoc node - same "click grid item -> preview popup with an upgrade
    // button" pattern as MeteoriteResearchPreviewLoc/FirestoneResearchPreviewLoc above.
    public static class TalentPreviewLoc
    {
        public const string Root = MenusLoc.Root + "/popups/TalentPreview";

        public const string CloseBtn = Root + "/bg/closeButton";

        // Live-confirmed, 2026-09-24, Steam-0: the preview's displayed talent name - used to verify
        // TalentTreeData's node names against the real game.
        public const string NameTxt = Root + "/bg/talentName";

        // Live-confirmed, 2026-09-24: reads "Level N/M" (e.g. "Level 1/25") - see
        // Talents.PreviewCurrentRank/GameText.GetParsedFirstInt for the parsing this needs.
        public const string LevelTxt = Root + "/bg/talentLevelText";

        // Shown instead of talentUpgrades when this tier's prerequisites aren't met yet.
        public const string LockedRoot = Root + "/bg/talentLocked";

        public const string UpgradeBtn = Root + "/bg/talentUpgrades/upgradeTalentButton";

        // NEVER click: removes an already-invested point (undo) - not part of this task's job.
        public const string DowngradeBtnDoNotUse = Root + "/bg/talentUpgrades/downgradeTalentButton";
    }
}
