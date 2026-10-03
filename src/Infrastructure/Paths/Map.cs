namespace Firebot.Infrastructure;

/// <summary>The World Map screen, its two tabs and popups, and the mission pins on the map layer.</summary>
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

            public const string DailyMissionsBtn = SubmenuRoot + "/dailyMissionsButton";

            public const string DailyMissionsNotification = DailyMissionsBtn + "/notification";
        }
    }

    // Opened by WarfrontLoc.DailyMissionsBtn. Only the liberation missions are wired, not the dungeons.
    public static class WFDailyMissionsLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFDailyMissions";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string OpenLiberationMissionsBtn = Root + "/bg/liberationMissions/openButton";

        public const string NextRunTimeTxt = Root + "/bg/timeLeftMain/timeLeftText";
    }

    // 10 fight-for-reward missions, no currency involved.
    public static class WFLiberationMissionsLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFLiberationMissions";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string MissionsGridRoot = Root + "/bg/missionsScrollView/Viewport/missionGrid";

        // Relative to a liberationMission (N); not clickable once locked or already won.
        public const string FightBtn = "/fightButton";
    }

    // The formation preview. The formation is set up by hand once; only Fight is ever pressed.
    public static class WFBattleSimLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFBattleSim";

        public const string FightBtn = Root + "/bg/mask/fightBtn";
    }

    // The campaign's 90 pins (WFCampaignMissionMapInteraction), on the map layer like MissionPinLoc's.
    // Only the next mission's pin is shown, but a hidden pin still opens its preview.
    public static class WFCampaignLoc
    {
        public const string PinsRoot = "menusRoot/mapRoot/mapElements/warfrontCampaignMissions";
    }

    // Opened by a campaign pin. One column per difficulty under ModesRoot (WFCampaignModeInteraction),
    // whose fightButton is clickable only when unlocked and the squad's power is enough.
    public static class WFCampaignMissionPreviewLoc
    {
        private const string Root = MenusLoc.Root + "/popups/WFCampaignMissionPreview";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string ModesRoot = Root + "/bg/modes";

        // "Battle formation": opens SelectWarMachinesLoc.
        public const string ChangeFormationBtn = Root + "/bg/changeFormationButton";
    }

    // The active squad, the one the campaign, Daily Missions and the Arena all fight with. Spots
    // (WarMachineFormationSettingSpot, spotIndex 0-4) and deck cards (WarMachineSelectInteraction).
    public static class SelectWarMachinesLoc
    {
        private const string Root = MenusLoc.Root + "/menus/SelectWarMachines";

        public const string CloseBtn = Root + "/closeButton";

        // Clickable only with unsaved changes.
        public const string SaveBtn = Root + "/bg/formationData/saveChanges";

        public const string SpotsRoot = Root + "/bg/formationSpots";

        public const string DeckRoot = Root + "/bg/warmachinesDeck/warMachinesScroll/Viewport/grid";
    }

    // A spot's crew, opened by its crew/editButton (or addButton). Lists that crew's heroes (selected)
    // and the free ones (HeroInteractionSelect).
    public static class SelectWarMachineHeroesLoc
    {
        private const string Root = MenusLoc.Root + "/popups/SelectWarMachineHeroes";

        public const string CloseBtn = Root + "/bg/closeButton";

        // "Save changes".
        public const string SaveBtn = Root + "/bg/setCrewButton";

        public const string HeroGridRoot = Root + "/bg/heroListScroll/Viewport/grid";
    }

    // The live battle, under menus/ unlike its siblings. A fightBtn can open it directly, skipping
    // WFBattleSim (a campaign fightButton always does). Never click its closeButton - that forfeits the battle.
    public static class WFBattleLoc
    {
        public const string Root = MenusLoc.Root + "/menus/WFBattle";
    }

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

    // Opened by clicking a mission pin.
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

    // The pins live on the map layer (menusRoot/mapRoot), not on the menu canvas.
    public static class MissionPinLoc
    {
        public const string Root = "menusRoot/mapRoot/mapElements/missions";

        // Relative to a pin.
        public const string ActiveIcon = "/missionActiveIcon";

        public const string TimeReq = "/missionBg/missionTimeBg/missionTimeReq";

        public const string Tick = "/missionBg/completedTick";
    }
}
