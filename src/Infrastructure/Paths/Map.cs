namespace Firebot.Infrastructure;

/// <summary>World Map screen (menus/WorldMap), its two tabs, related popups, and the mission pins
/// that live on the always-visible background map layer.</summary>
public static partial class Paths
{
    public static class WorldMapLoc
    {
        private const string Root = MenusLoc.Root + "/menus/WorldMap";

        public const string CloseBtn = Root + "/closeButton";

        public const string MapMissionsTabBtn = Root + "/submenuButtons/mapMissionsButton";

        public const string WarfrontCampaignTabBtn = Root + "/submenuButtons/warfrontCampaignButton";

        public static class MapMissionsLoc
        {
            private const string Root =
                WorldMapLoc.Root + "/submenus/mapMissionsSubmenu/bottomLeftUI/missionRefreshCanvas";

            public const string NextRunTimeTxt = Root + "/missionRefreshBg/missionRefreshText";
        }

        public static class WarfrontLoc
        {
            private const string SubmenuRoot = WorldMapLoc.Root + "/submenus/warfrontCampaignSubmenu";

            private const string LootBtn = SubmenuRoot + "/loot";

            public const string NextRunTimeTxt = LootBtn + "/nextLootTimeLeft";

            public const string ClaimBtn = LootBtn + "/claimButton";

            // this path existed before but was never wired any task to it - see WFDailyMissionsLoc/
            // WFLiberationMissionsLoc below for the popups it opens.
            public const string DailyMissionsBtn = SubmenuRoot + "/dailyMissionsButton";

            public const string DailyMissionsNotification = DailyMissionsBtn + "/notification";
        }
    }

    // Hub popup opened by WarfrontLoc.DailyMissionsBtn - has two categories (liberationMissions,
    // dungeonMissions), only the first of which had a path defined before. dungeonMissions found
    // via a fresh UnityPy scan but not yet wired to anything - out of scope for now.
    public static class WFDailyMissionsLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFDailyMissions";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string OpenLiberationMissionsBtn = Root + "/bg/liberationMissions/openButton";

        public const string NextRunTimeTxt = Root + "/bg/timeLeftMain/timeLeftText";
    }

    // Opened by WFDailyMissionsLoc.OpenLiberationMissionsBtn - a grid of 10 fight-for-reward
    // missions, no currency/cost involved anywhere in this popup (confirmed via UnityPy scan).
    public static class WFLiberationMissionsLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFLiberationMissions";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string MissionsGridRoot = Root + "/bg/missionsScrollView/Viewport/missionGrid";

        // Relative to a liberationMission (N) child - safe no-op via IsClickable() if locked/already won.
        public const string FightBtn = "/fightButton";
    }

    // Opened by WFLiberationMissionsLoc.FightBtn - a squad/formation preview with the actual "start"
    // button (fightBtn). Formation (changeAttackerFormationButton) is set up once manually by the
    // user per their instructions - only FightBtn is ever wired here.
    public static class WFBattleSimLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFBattleSim";

        public const string FightBtn = Root + "/bg/mask/fightBtn";
    }

    // Real-time battle screen opened by WFBattleSimLoc.FightBtn, resolving into either
    // WFBattleWonLoc or WFBattleDefeatLoc. No path defined for WFBattle itself - nothing on it needs
    // clicking (its own closeButton would forfeit mid-battle); the bot only waits for a result popup.
    public static class WFBattleWonLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFBattleWon";

        public const string CloseBtn = Root + "/bg/closeButton";
    }

    public static class WFBattleDefeatLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFBattleDefeat";

        public const string CloseBtn = Root + "/bg/closeButton";
    }

    // Popup shown when a mission pin is clicked - lives under menuCanvas/popups, not menus/WorldMap.
    public static class PreviewMissionLoc
    {
        private const string Root = MenusLoc.Root + "/popups/PreviewMission";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string StartBtn = Root + "/bg/managementBg/container/startMissionButton";

        public const string SpeedupBtn = Root + "/bg/managementBg/container/speedUpButton";

        public const string SpeedupFinishDesc = SpeedupBtn + "/finishDesc";

        public const string NotEnoughSquadsTxt = Root + "/bg/managementBg/previewMissionNotEnoughSquads";

        public const string NextRunTimeTxt =
            Root + "/bg/rewardBg/previewMissionTime/previewBar/missionProgress/activeMissionProgressText";
    }

    public static class MissionRewardsLoc
    {
        private const string Root = MenusLoc.Root + "/popups/MissionRewards";

        public const string CloseBtn = Root + "/bg/closeButton";
    }

    public static class MissionPinLoc
    {
        // Lives under menusRoot/mapRoot (the always-visible background map layer), NOT under
        // menuCanvas/menus like every other screen in this file - confirmed against an already
        // proven, live-tested path. MenusLoc.Root can't be reused here since it already includes the
        // menuCanvasParent/SafeArea/menuCanvas suffix that this branch doesn't have.
        public const string Root = "menusRoot/mapRoot/mapElements/missions";

        public const string ActiveIcon = "/missionActiveIcon";

        public const string TimeReq = "/missionBg/missionTimeBg/missionTimeReq";

        public const string Tick = "/missionBg/completedTick";
    }
}
