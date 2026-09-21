namespace Firebot.Infrastructure;

/// <summary>
///     Forbidden Knowledge (https://firestone-idle-rpg.fandom.com/wiki/Forbidden_Knowledge) - 3
///     separate, parallel upgrade trees (Kramatak/Ledra/Yamanoth - Ocean secrets/Chaos
///     teachings/Thunder chronicles), each spending only that SAME god's Tome of Power (earned via
///     Chaos Rift, see ChaosRiftLoc/ChaosRiftShopLoc) - never a shared currency across boards.
///     Unlocks at character level 100 per the wiki infobox (same as Chaos Rift). Each board also has
///     a "Recruit [God]" button, live-confirmed by the wiki (Ledra only, presumed the same for the
///     other two): costs 50 of that god's tomes, only available once every node on that board is
///     already maxed - its own IsClickable() already encodes both requirements, so this is a plain
///     no-op-safe click attempt, same principle used for every other "advanced" action in this
///     codebase. Reached from Town -&gt; Guild -&gt; TownGuildLoc.ForbiddenKnowledgeBtn.
///     Paths cross-checked against a UnityPy dump (docs/screens/ForbiddenKnowledge.html) via a
///     research pass, then live-confirmed, 2026-09-20: Root itself and its direct children
///     (attributes/purchaseGod/submenuButtons/closeButton) were all correct on the first try - unlike
///     ChaosRiftLoc's sibling screen, no extra nesting wrapper here. The node grid itself (properly
///     indexed "forbiddenKnoweledgeUpgrade (0)".."(9)", note the game's own typo "Knoweledge") is
///     dump-confirmed too - unlike Pirate's Prize's tier list this isn't the duplicate-sibling-name
///     situation. The one wrong guess: the upgrade preview popup's root is "ForbiddenKnowledgePreview"
///     (found live under "popups/"), not "ForbiddenKnowledgeUpgradePreview" as first guessed from
///     naming convention alone. Its buy button turned out to be nested 3 levels deeper than expected -
///     bg/innerBg/normal/buyUpgradeButton, not a direct child - found via 3 rounds of live diagnostics,
///     2026-09-20 (the "normal" segment is presumably a normal-vs-already-maxed state container).
/// </summary>
public static partial class Paths
{
    public static class ForbiddenKnowledgeLoc
    {
        public const string Root = MenusLoc.Root + "/menus/ForbiddenKnowledge";

        public const string CloseBtn = Root + "/closeButton";

        public const string RecruitBtn = Root + "/purchaseGod";

        public const string KramatakTabBtn = Root + "/submenuButtons/kramatak";
        public const string LedraTabBtn = Root + "/submenuButtons/ledra";
        public const string YamanothTabBtn = Root + "/submenuButtons/yamanoth";

        private const string NodesRoot = Root + "/attributes";

        public const int NodeCount = 10;

        public static string NodeBtn(int index) => $"{NodesRoot}/forbiddenKnoweledgeUpgrade ({index})";

        // Live-confirmed, 2026-09-20 (see class doc comment for the naming-guess correction).
        public const string PreviewRoot = MenusLoc.Root + "/popups/ForbiddenKnowledgePreview";

        // Live-confirmed, 2026-09-20: the close button lives under a "bg" child, not directly under
        // the root (that first guess is why the close click never landed either).
        private const string PreviewBg = PreviewRoot + "/bg";

        public const string PreviewCloseBtn = PreviewBg + "/closeButton";

        // Live-confirmed, 2026-09-20, after 3 rounds of diagnostics: the buy button is nested 3 levels
        // below the popup root - bg/innerBg/normal/buyUpgradeButton (not "upgradeButton" as first
        // guessed, and not directly under "bg" or "innerBg" either).
        public const string PreviewUpgradeBtn = PreviewBg + "/innerBg/normal/buyUpgradeButton";
    }
}
