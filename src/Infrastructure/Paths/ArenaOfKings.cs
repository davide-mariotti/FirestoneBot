namespace Firebot.Infrastructure;

/// <summary>Arena of Kings (war machine PvP): Town -> Battles -> WFMenuSelection -> arena.</summary>
public static partial class Paths
{
    // Opened by TownIrongardLoc.BattlesBtn; the other option is the Warfront campaign.
    public static class WFMenuSelectionLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFMenuSelection";

        public const string ArenaBtn = Root + "/bg/arena";
    }

    public static class ArenaOfKingsLoc
    {
        private const string Root = MenusLoc.Root + "/menus/ArenaOfKings";

        public const string CloseBtn = Root + "/closeButton";

        public const string BattleTokensTxt = Root + "/bg/battleTokens/quantity";

        // The "Arena power" figure (arena battles use their own power formula), label included.
        public const string MyArenaPowerTxt = Root + "/bg/battleFormation/totalPower";

        public const string RerollBtn = Root + "/bg/opponentHub/refreshBg/refreshButton";

        public const string OpponentGridRoot = Root + "/bg/opponentHub/opponentGrid";

        // Relative to one of the 3 opponent slots.
        public const string OpponentPowerTxt = "/powerBg/powerValue";

        public const string OpponentFightBtn = "/fightButton";
    }

    // The formation preview. The formation is set up by hand once; only Fight is ever pressed.
    public static class AOKBattlePreviewLoc
    {
        private const string Root = MenusLoc.Root + "/popups/AOKBattlePreview";

        public const string FightBtn = Root + "/bg/mask/fightButton";
    }

    // One result popup for both outcomes, with a single close button.
    public static class AOKBattleResultLoc
    {
        private const string Root = MenusLoc.Root + "/popups/AOKBattleResult";

        public const string CloseBtn = Root + "/bg/closeButton";
    }
}
