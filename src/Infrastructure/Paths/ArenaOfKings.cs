namespace Firebot.Infrastructure;

/// <summary>
///     Arena of Kings - PvP war machine battles, unlocking at character level 80 per the wiki. Reached
///     from Town -&gt; TownIrongardLoc.BattlesBtn -&gt; WFMenuSelectionLoc (a 2-option hub: campaign/arena)
///     -&gt; ArenaOfKingsLoc. This whole feature is new, found via a targeted UnityPy
///     scan requested by the user.
/// </summary>
public static partial class Paths
{
    // Hub popup opened by TownIrongardLoc.BattlesBtn - shares this same entry point with Warfront
    // Campaign (the "campaign" option), confirmed via UnityPy. Only "arena" is wired here.
    public static class WFMenuSelectionLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFMenuSelection";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string ArenaBtn = Root + "/bg/arena";
    }

    public static class ArenaOfKingsLoc
    {
        private const string Root = MenusLoc.Root + "/menus/ArenaOfKings";

        public const string CloseBtn = Root + "/closeButton";

        public const string BattleTokensTxt = Root + "/bg/battleTokens/quantity";

        // Live-confirmed, 2026-09-18: the UnityPy-guessed "arenaPower" name doesn't exist -
        // battleFormation's real children are title/totalPower/warMachineGrid/decorDetail(x2), and
        // "totalPower" holds its TMP_Text component directly (no nested "text" child, unlike most
        // other counters in this codebase). This is the same figure the screen itself labels "Arena
        // power" (arena battles use a different power formula than regular campaign battles, per the
        // wiki).
        public const string MyArenaPowerTxt = Root + "/bg/battleFormation/totalPower";

        public const string RerollBtn = Root + "/bg/opponentHub/refreshBg/refreshButton";

        public const string OpponentGridRoot = Root + "/bg/opponentHub/opponentGrid";

        // Relative to an opponent slot (arenaOfKingsOpponentInteraction (0-2), always exactly 3 per
        // the wiki: "choose to fight one of 3 player opponents").
        public const string OpponentPowerTxt = "/powerBg/powerValue";

        public const string OpponentFightBtn = "/fightButton";
    }

    // Opened by an opponent's OpponentFightBtn - the squad/formation preview with the actual "start"
    // button, same pattern as WFBattleSimLoc (Warfront) - formation is set up once manually by the
    // user, only FightBtn is ever wired here.
    public static class AOKBattlePreviewLoc
    {
        private const string Root = MenusLoc.Root + "/popups/AOKBattlePreview";

        public const string FightBtn = Root + "/bg/mask/fightButton";
    }

    // The real-time battle itself has no path defined here at all (same reasoning as WFBattleLoc -
    // nothing on it needs clicking, waiting is all that's needed) - confirmed via the wiki that
    // results are server-determined and then shown as a "recording", so the actual wait here may be
    // short and fairly consistent, but bounded-poll for it anyway rather than assume a fixed length.
    // Resolves into AOKBattleResultLoc - unlike Warfront's separate Won/Defeat popups, this is ONE
    // popup with both a "won" and "lost" section (only one visible) and a single shared close button.
    public static class AOKBattleResultLoc
    {
        private const string Root = MenusLoc.Root + "/popups/AOKBattleResult";

        public const string CloseBtn = Root + "/bg/closeButton";
    }
}
