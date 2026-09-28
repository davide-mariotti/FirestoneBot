namespace Firebot.Infrastructure;

/// <summary>
///     Forbidden Knowledge: three boards (Kramatak, Ledra, Yamanoth), each spending only its own
///     god's Tomes of Power (bought in the Chaos Rift shop). "Recruit" needs the whole board maxed
///     plus 50 tomes, which the button's own clickable state already reflects.
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

        // "Knoweledge" is the game's own spelling - don't correct it.
        public static string NodeBtn(int index) => $"{NodesRoot}/forbiddenKnoweledgeUpgrade ({index})";

        // The node preview popup that holds the actual buy button.
        private const string PreviewRoot = MenusLoc.Root + "/popups/ForbiddenKnowledgePreview";

        private const string PreviewBg = PreviewRoot + "/bg";

        public const string PreviewCloseBtn = PreviewBg + "/closeButton";

        public const string PreviewUpgradeBtn = PreviewBg + "/innerBg/normal/buyUpgradeButton";
    }
}
