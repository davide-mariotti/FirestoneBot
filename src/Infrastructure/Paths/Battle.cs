namespace Firebot.Infrastructure;

/// <summary>The battle screen's HUD. See UiVariantButton for why most buttons are listed under several roots.</summary>
public static partial class Paths
{
    public static class BattleLoc
    {
        private const string Root = "battleRoot/battleMain/battleCanvas/SafeArea";

        public static class PlayerAvatarLoc
        {
            // The avatar itself is the button that opens the Character screen.
            public const string OpenBtn = BattleLoc.Root + "/topLeftSideUI/playerAvatar";

            public const string CharacterLevel = OpenBtn + "/characterLevelBg/characterLevel";
        }

        public static class LeftSideUINewLoc
        {
            public const string Root = BattleLoc.Root + "/leftSideUINew";

            public const string MailBtn = Root + "/mail";

            // Not verified live: guessed from Path of Glory's badge, which sits on its own button.
            public const string MailNotification = MailBtn + "/notification";
        }

        // Fallback variant of LeftSideUINewLoc - never seen populated in a live session so far.
        public static class BottomLeftSideUILoc
        {
            private const string Root = BattleLoc.Root + "/bottomLeftSideUI";

            public const string MailBtn = Root + "/mail";

            public const string MailNotification = MailBtn + "/notification";
        }

        /// <summary>
        ///     Badge names on the notification rail - a badge is shown only while its activity has
        ///     something to claim or do. Every name here was confirmed against a live dump of the rail's
        ///     48 badges. The rail sits under Root or FallbackRoot depending on the client.
        /// </summary>
        public static class NotificationsLoc
        {
            public const string Root = LeftSideUINewLoc.Root + "/notifications/Viewport/grid";

            public const string FallbackRoot = BattleLoc.Root + "/leftSideUI/notifications/Viewport/grid";

            public const string OraclesGift = "OraclesGift";

            public const string CheckIn = "CheckIn";

            public const string MysteryBox = "MysteryBox";

            public const string Quests = "Quests";

            public const string FreePickaxes = "FreePickaxes";

            public const string Engineer = "Engineer";

            public const string Expeditions = "Expeditions";

            public const string GuardianTraining = "GuardianTraining";

            public const string OracleRituals = "OracleRituals";

            public const string Experiments = "Experiments";

            public const string WarfrontCampaign = "WarfrontCampaign";

            public const string MapMissions = "MapMissions";

            public const string FirestoneResearch = "FirestoneResearch";

            public const string TemplePrestige = "TemplePrestige";

            public const string MeteoriteResearch = "MeteoriteResearch";

            public const string ScarabGame = "ScarabGame";

            public const string ScarabGameShopFreeToken = "ScarabGameShopFreeToken";

            public const string ArcaneCrystal = "ArcaneCrystal";

            public const string BeerExchange = "BeerExchange";

            public const string TalentAvailable = "TalentAvailable";

            public const string ArenaTokens = "ArenaTokens";

            public const string HallOfHeroes = "HallOfHeroes";

            public const string PiratesPrize = "PiratesPrize";

            public const string ChaosRift = "ChaosRift";

            // Lit while there's Dark Rune to spend in the Chaos Rift supplies shop.
            public const string ChaosRiftSupplies = "ChaosRiftSupplies";

            public const string ForbiddenKnowledge = "ForbiddenKnowledge";

            public const string Awakening = "Awakening";

            public const string Warmachines = "Warmachines";

            public const string ScarabGameMilestones = "ScarabGameMilestones";

            // Opens Magic Quarters on its Chaos Rift (holy damage) tab.
            public const string GuardianHolyUpgrade = "GuardianHolyUpgrade";
        }

        public static class RightSideUILoc
        {
            private const string Root = BattleLoc.Root + "/rightSideUI/menuButtons";

            public const string StoreBtn = Root + "/storeButton";

            public const string GuildBtn = Root + "/guildButton";

            public const string TownBtn = Root + "/townButton";

            public const string MapBtn = Root + "/mapButton";

            public const string PathOfGloryBtn = Root + "/pathOfGloryButton";

            public const string PathOfGloryNotification = PathOfGloryBtn + "/notification";

            public const string EventsBtn = Root + "/eventsButton";

            // One badge for every event: lit while any of them has something to claim.
            public const string EventsNotification = EventsBtn + "/notification";
        }

        public static class BottomRightSideUINewLoc
        {
            private const string Root = BattleLoc.Root + "/bottomRightSideUINew/menuButtons";

            public const string InventoryBtn = Root + "/inventoryButtonUI";

            // Not verified live: assumed from InventoryBtn in the same row.
            public const string PartyBtn = Root + "/partyButtonUI";
        }

        public static class StageProgressionLoc
        {
            private const string Root = BattleLoc.Root + "/topSideUI/stageProgression";

            public const string CurrentStageNumTxt = Root + "/currentStage/stageNum";

            public const string GoBackBtn = Root + "/goBackStage";
        }

        public static class BottomSideUIDesktopLoc
        {
            private const string Root = BattleLoc.Root + "/bottomSideUIDesktop";

            public const string PathOfGloryBtn = Root + "/pathOfGloryButton";

            // Battle Pass has no badge on the notification rail - its button carries its own.
            public const string PathOfGloryNotification = PathOfGloryBtn + "/notification";

            public const string InventoryBtn = Root + "/menuButtons/inventoryButtonUI";

            // Not verified live: assumed from InventoryBtn in the same row.
            public const string PartyBtn = Root + "/menuButtons/partyButtonUI";

            // A direct child here, but under offersLayout in the Mobile variant.
            public const string EventsBtn = Root + "/eventsButton";
        }

        public static class BottomSideUIMobileLoc
        {
            private const string Root = BattleLoc.Root + "/bottomSideUIMobile";

            public const string PathOfGloryBtn = Root + "/offersLayout/pathOfGloryButton";

            public const string PathOfGloryNotification = PathOfGloryBtn + "/notification";

            public const string InventoryBtn = Root + "/menuButtons/inventoryButtonUI";

            public const string PartyBtn = Root + "/menuButtons/partyButtonUI";

            public const string EventsBtn = Root + "/offersLayout/eventsButton";

            public const string EventsNotification = EventsBtn + "/notification";
        }

        public static class BottomSideUINewLoc
        {
            private const string Root = BattleLoc.Root + "/bottomSideUINew/bgBlack";

            public static class LeaderPanelLoc
            {
                private const string Root = BottomSideUINewLoc.Root + "/LeaderPanel/leaderPanelNew";

                public const string LvlUpBtn = Root + "/lvlUpButtonBig";
            }

            public static class HeroSlotsLoc
            {
                public const string Root = BottomSideUINewLoc.Root + "/layout";

                // Relative to a hero slot.
                public const string LvlUpBtn = "/lvlUpButtonBig";
            }

            public static class ChangeLevelUpModeLoc
            {
                public const string Button = BottomSideUINewLoc.Root + "/rightSide/changeLevelUpModeButton";

                public const string Text = Button + "/text";
            }
        }
    }
}
