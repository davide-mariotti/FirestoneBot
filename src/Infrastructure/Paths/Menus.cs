namespace Firebot.Infrastructure;

/// <summary>
///     Scene paths of the game's UI, one partial class per screen area. A path is verified live
///     unless a comment says otherwise. Paths starting with "/" are relative to a parent element
///     (a list item, a slot) and only make sense combined with it.
/// </summary>
public static partial class Paths
{
    /// <summary>Screens and popups opened over the battle view.</summary>
    public static class MenusLoc
    {
        internal const string Root = "menusRoot/menuCanvasParent/SafeArea/menuCanvas";

        public static class StoreLoc
        {
            private const string Root = MenusLoc.Root + "/menus/Store";

            public const string CloseBtn = Root + "/closeButton";

            public static class TabsLoc
            {
                private const string Root = StoreLoc.Root + "/submenuButtons/grid";

                public const string DailyRewardsBtn = Root + "/dailyRewardsButton";

                public const string ValueBundleDailyBtn = Root + "/valueBundleDailyButton";

                // "Special packs" - a separate tab with its own free mystery box (ExtremeValueBundlesLoc).
                public const string ValueBundleBtn = Root + "/valueBundleButton";
            }

            public static class DailyRewardsLoc
            {
                private const string Root = StoreLoc.Root + "/bg/submenus/dailyRewards/Viewport/transparentFrame";

                public const string CheckInBtn = Root + "/checkIn";

                public const string NextRunTimeTxt = Root + "/textHolder/timer";
            }

            public static class ValueBundleDailyLoc
            {
                private const string Root = StoreLoc.Root + "/bg/submenus/valueBundleDaily";

                // The one free slot. The numbered valueBundle (0)/(1)/(2) slots beside it cost real
                // money - never click those.
                public const string FreeMysteryBoxBtn =
                    Root + "/Scroll View/Viewport/bundles/mysteryBox/Graphics/purchaseButton";

                public const string RenewTxt = Root + "/timeRenewBackground/renewText";
            }

            public static class ExtremeValueBundlesLoc
            {
                private const string Root = StoreLoc.Root + "/bg/submenus/extremeValueBundles/bundles";

                // Claimed independently of ValueBundleDailyLoc's box. The sibling
                // "extremeValueBundle" card costs real money - never click it.
                public const string FreeMysteryBoxBtn = Root + "/mysteryBox/Graphics/purchaseButton";
            }
        }

        public static class CharacterLoc
        {
            internal const string Root = MenusLoc.Root + "/popups/Character";

            public const string CloseBtn = Root + "/bg/closeButton";

            public const string QuestsTabBtn = Root + "/bg/submenuButtons/quests";

            public const string TalentsTabBtn = Root + "/bg/submenuButtons/talents";

            public static class QuestsLoc
            {
                private const string Root = CharacterLoc.Root + "/bg/submenus/quests";

                public const string DailyTabBtn = Root + "/bg/submenuButtons/dailyButton";

                public const string WeeklyTabBtn = Root + "/bg/submenuButtons/weeklyButton";

                public const string DailyQuestsGridRoot = Root + "/bg/submenus/dailyQuestsScroll/Viewport/grid";

                public const string WeeklyQuestsGridRoot = Root + "/bg/submenus/weeklyQuestsScroll/Viewport/grid";

                // Shared by both tabs: shows the countdown of whichever tab is selected.
                public const string RenewTxt = Root + "/questsRenewBg/questsRenewText";
            }
        }

        public static class TownGuildLoc
        {
            private const string Root = MenusLoc.Root + "/menus/TownGuild";

            public const string CloseBtn = Root + "/closeButton";

            public const string GuildShopBtn = Root + "/guildShop";

            public const string ExpeditionsBtn = Root + "/expeditions";

            public const string ArcaneCrystalBtn = Root + "/arcaneCrystal";

            public const string TreeOfLifeBtn = Root + "/treeOfLife";

            public const string AwakeningBtn = Root + "/awakening";

            public const string ChaosRiftBtn = Root + "/chaosRift";

            // Not verified live: guessed from the sibling icons' naming.
            public const string ForbiddenKnowledgeBtn = Root + "/forbiddenKnowledge";
        }

        public static class ExpeditionsLoc
        {
            private const string Root = MenusLoc.Root + "/popups/Expeditions";

            public const string CloseBtn = Root + "/bg/closeButton";

            public const string NextRunTimeTxt = Root + "/bg/timeLeftBg/timeLeftText";

            public const string ActiveExpedition =
                Root + "/bg/expeditionsParent/activeExpeditionParent/activeExpedition";

            public const string ClaimBtn = ActiveExpedition + "/claimButton";

            public const string CurrentRunTimeTxt = ActiveExpedition + "/expeditionProgressBg/timeLeftText";

            // The first pending expedition in the list.
            public const string StartBtn =
                Root +
                "/bg/expeditionsParent/pendingExpeditionsParent/expeditionsScroll/Viewport/grid/expeditionPending0/startButton";
        }

        public static class TownIrongardLoc
        {
            // menus/, not popups/ - that guess has been made and reverted before.
            private const string Root = MenusLoc.Root + "/menus/TownIrongard";

            public const string CloseBtn = Root + "/closeButton";

            public const string EngineerBtn = Root + "/townBg/parent/engineer";

            public const string MagicQuartersBtn = Root + "/townBg/parent/magicQuarters";

            public const string OracleBtn = Root + "/townBg/parent/oracle";

            public const string AlchemistBtn = Root + "/townBg/parent/alchemist";

            public const string LibraryBtn = Root + "/townBg/parent/library";

            public const string TempleOfEternalsBtn = Root + "/townBg/parent/templeOfEternals";

            public const string TavernBtn = Root + "/townBg/parent/tavern";

            public const string ExoticMerchantBtn = Root + "/townBg/parent/exoticMerchant";

            // Opens WFMenuSelection (campaign / arena).
            public const string BattlesBtn = Root + "/townBg/parent/battles";

            public const string HallOfHeroesBtn = Root + "/townBg/parent/hallOfHeroes";

            // "ship", not "pirateShip". A separate "merchantShip" icon is a different feature.
            public const string PirateShipBtn = Root + "/townBg/parent/ship";
        }

        // The tavern building opens this choice between the card game and Scarab's Game.
        public static class TavernSelectionLoc
        {
            private const string Root = MenusLoc.Root + "/popups/TavernSelection";

            private const string CardsRoot = Root + "/bg";

            public const string OpenTavernBtn = CardsRoot + "/tavern";

            public const string OpenScarabGameBtn = CardsRoot + "/scarabGame";
        }

        public static class TavernLoc
        {
            private const string Root = MenusLoc.Root + "/menus/Tavern";

            public const string CloseBtn = Root + "/closeButton";

            // Opens TavernMarket ("Stormy, the tavern keeper").
            public const string OpenMarketBtn = Root + "/helpCanvas/stormyButton";

            public const string PlayBtn = Root + "/helpCanvas/bottomUI/playButton";

            // Face-down cards tavernCard0..5 appear after Play; any one completes the round.
            private const string CardsRoot = Root + "/helpCanvas/cardHolder";

            public const string FirstCardBtn = CardsRoot + "/tavernCard0";

            public const string ChangeQuantityBtn = Root + "/helpCanvas/bottomRightUI/changeQuantity";

            public const string QuantityTxt = ChangeQuantityBtn + "/text";

            // The real counter. A sibling "counterInteraction" exists too but always reads empty here.
            public const string GameTokenCountTxt = Root + "/helpCanvas/counters/currencyInteraction (GameToken)/quantity";
        }

        public static class TempleOfEternalsLoc
        {
            private const string Root = MenusLoc.Root + "/menus/TempleOfEternals";

            private const string PrestigeSubmenuRoot = Root + "/submenus/bgNew/prestigeSubmenu";

            public const string CloseBtn = Root + "/closeButton";

            public const string EmpowerBtn = PrestigeSubmenuRoot + "/adventureInfo/openEmpowerButton";

            public const string AdventureTimePlayedTxt = PrestigeSubmenuRoot + "/adventureInfo/adventureTimePlayed";

            public const string FirestonesFoundTxt = PrestigeSubmenuRoot + "/adventureInfo/firestonesFound";

            public const string FirestonesYouOwnTxt =
                PrestigeSubmenuRoot + "/progress/firestonesYouOwnBg/firestonesYouOwn";
        }

        // Confirmation popup opened by TempleOfEternalsLoc.EmpowerBtn.
        public static class EmpowerPopupLoc
        {
            private const string Root = MenusLoc.Root + "/popups/EmpowerPopup";

            public const string EmpowerBtn = Root + "/bg/empowerBg/empowerButton";
        }

        // Generic "are you sure?" gate the Empower flow goes through.
        public static class ActionRequiredLoc
        {
            private const string Root = MenusLoc.Root + "/popups/ActionRequired";

            public const string ConfirmBtn = Root + "/bg/confirmButton";
        }

        // Shown right after a successful empower.
        public static class TOEPrestigeCompleteLoc
        {
            private const string Root = MenusLoc.Root + "/popups/TOEPrestigeComplete";

            public const string ConfirmBtn = Root + "/bg/confirmButton";
        }

        // The Engineer building opens this choice first. War Machines live behind "garage".
        public static class GarageSelectionLoc
        {
            private const string Root = MenusLoc.Root + "/popups/GarageSelection/bg";

            public const string OpenEngineerBtn = Root + "/engineer";

            public const string OpenGarageBtn = Root + "/garage";
        }

        public static class EngineerLoc
        {
            private const string Root = MenusLoc.Root + "/menus/Engineer";

            public const string CloseBtn = Root + "/closeButton";

            public const string ClaimBtn = Root + "/submenus/bg/engineerSubmenu/toolsProductionSection/claimToolsButton";

            public const string NextRunTimeTxt = ClaimBtn + "/cooldownOn/cooldownTimeLeft";
        }

        public static class MagicQuartersLoc
        {
            private const string Root = MenusLoc.Root + "/menus/MagicQuarters";

            public const string CloseBtn = Root + "/closeButton";

            public const string GuardiansRoot = Root + "/guardianList";

            // Relative to a guardian in GuardiansRoot; shown only once that guardian is unlocked.
            public const string GuardianStarsIcon = "/starsParent";

            private const string UnlockedGuardianRoot = Root + "/submenus/bg/infoSubmenu/activities/unlocked";

            public const string EnlightenmentBtn = UnlockedGuardianRoot + "/enlightenment/enlightenmentButton";

            public const string TrainBtn = UnlockedGuardianRoot + "/train/trainButton";

            public const string NextRunTimeTxt = TrainBtn + "/cooldownOn/cooldownTimeLeft";

            // Tabs: info (training), evolution, chaosRift, rarity, skin.
            private const string SubmenusRoot = Root + "/submenus";

            public const string ChaosRiftTabBtn = SubmenusRoot + "/submenuButtons/chaosRift";

            private const string ChaosRiftSubmenuRoot = SubmenusRoot + "/bg/chaosRiftSubmenu";

            // Spends Orbs of Light on holy damage; they reset monthly, so there's no reason to save them.
            public const string ChaosRiftUpgradeBtn = ChaosRiftSubmenuRoot + "/holyDamageUpgrade/bg/upgradeButton";
        }

        // Shown when a still-locked guardian is clicked.
        public static class LockedGuardianLoc
        {
            private const string Root = MenusLoc.Root + "/popups/LockedGuardian";

            public const string CloseBtn = Root + "/bg/closeButton";
        }

        // Generic validation toast ("You need to complete tree I first", ...). It's a sibling of the
        // screen that raised it, so closing it leaves that screen open.
        public static class GenericMessageLoc
        {
            private const string Root = MenusLoc.Root + "/popups/GenericMessage";

            public const string CloseBtn = Root + "/bg/closeButton";
        }

        // "You need N more <currency>" - a different popup from GenericMessage.
        public static class CurrencyMissingLoc
        {
            private const string Root = MenusLoc.Root + "/popups/CurrencyMissing";

            public const string CloseBtn = Root + "/bg/closeButton";
        }

        public static class GuildShopLoc
        {
            private const string Root = MenusLoc.Root + "/menus/GuildShop";

            public const string CloseBtn = Root + "/closeButton";

            public const string SuppliesTabBtn = Root + "/bg/submenuButtons/supplies/button";

            public static class FreePickaxeLoc
            {
                private const string Root = GuildShopLoc.Root + "/bg/submenus/supplies/items/freePickaxe";

                // The freePickaxe row has no Button of its own; this nested one ("free") is the target.
                public const string ClaimBtn = Root + "/claimBg/purchaseButton";

                public const string QuantityTxt = Root + "/claimBg/itemBg/itemQuantity";

                public const string NextRunTimeTxt = Root + "/nextFreeObj/progressBarBg/timeLeftText";
            }
        }

        // The Oracle building (rituals). Not OracleStoreLoc, which the OraclesGift badge opens.
        public static class OracleLoc
        {
            private const string Root = MenusLoc.Root + "/menus/Oracle";

            public const string CloseBtn = Root + "/closeButton";

            public static class RitualLoc
            {
                private const string Root = OracleLoc.Root + "/submenus/bg/ritualSubmenu";

                public const string Rituals = Root + "/ritualsGrid";

                // Relative to an oracleRitualInteraction (N) child of Rituals.
                public const string ClaimBtn = "/claimButton";

                public const string CurrentRunTimeTxt = "/ritualProgressBg/timeLeftText";

                public const string StartBtn = "/startButton";

                public const string NextRunTimeTxt = Root + "/timeBg/timeLeft";
            }
        }

        public static class AlchemistLoc
        {
            private const string Root = MenusLoc.Root + "/menus/Alchemist";

            public const string CloseBtn = Root + "/closeButton";

            public static class ExperimentsLoc
            {
                public const string Root = AlchemistLoc.Root + "/submenus/bg/experimentsSubmenu/experiments";

                // Relative to an alchExperimentType(N) child of Root.
                public const string StartBtn = "/startExperiment";

                // Relative to an alchExperimentSlot(N) child of Root.
                public const string ClaimBtn = "/claimButton";

                public const string NextRunTimeTxt = "/progressBarBg/timeLeftText";

                public const string SpeedupBtn = "/speedUpButton";

                public const string SpeedupFinishDesc = SpeedupBtn + "/finishDesc";
            }
        }

        public static class LibraryLoc
        {
            private const string Root = MenusLoc.Root + "/menus/Library";

            public const string CloseBtn = Root + "/closeButton";

            public const string FirestoneResearchTabBtn = Root + "/submenuButtons/firestoneResearch";

            public const string MeteoriteResearchTabBtn = Root + "/submenuButtons/meteoriteResearch";

            // The Meteorite balance, shared by Meteorite Research and hero gear tier unlocks.
            public const string MeteoriteBalanceTxt = Root + "/counters/counterInteraction/quantity";

            public static class ResearchPanelLoc
            {
                public const string Root = LibraryLoc.Root + "/submenus/firestoneResearch/researchPanel";

                public const string SelectResearchTable = Root + "/selectResearchTable";

                public const string UnlockSlotBtn = Root + "/unlockResearchSlot/confirmButton";

                // Relative to a researchSlot child of Root.
                public const string ClaimBtn = "/container/claimButton";

                public const string NextRunTimeTxt = "/container/researchInfo/progressBarBg/timeLeftText";

                public const string SpeedupBtn = "/container/speedUpButton";

                public const string SpeedupFinishDesc = SpeedupBtn + "/finishDesc";
            }

            // The Firestone Research node trees - a carousel, one tree visible at a time.
            public static class NodeLoc
            {
                private const string SubmenuRoot = LibraryLoc.Root + "/submenus/firestoneResearch";

                public const string Root = SubmenuRoot + "/researchScrollView/viewport/content/submenus";

                // Relative to a node.
                public const string Glow = "/glow";

                public const string ProgressBar = "/progressBarBg";

                public const string CompletedTxt = "/genericText";

                public const string NextTreeBtn = SubmenuRoot + "/navigation/goForthTree";

                public const string PreviousTreeBtn = SubmenuRoot + "/navigation/goBackTree";
            }

            // The Meteorite Research node trees - same carousel; a node click opens
            // MeteoriteResearchPreviewLoc with its cost.
            public static class MeteoriteResearchLoc
            {
                private const string SubmenuRoot = LibraryLoc.Root + "/submenus/meteoriteResearch";

                private const string NavigationRoot = SubmenuRoot + "/navigation";

                public const string NextTreeBtn = NavigationRoot + "/goForthTree";

                public const string PreviousTreeBtn = NavigationRoot + "/goBackTree";

                public const string TreesRoot = SubmenuRoot + "/submenus";
            }
        }

        public static class FirestoneResearchPreviewLoc
        {
            private const string Root = MenusLoc.Root + "/popups/FirestoneResearchPreview";

            public const string CloseBtn = Root + "/bg/closeButton";

            public const string NameTxt = Root + "/bg/innerBg/researchName";

            public const string LevelTxt = Root + "/bg/innerBg/researchLevelText";

            public const string UnlockedTxt = Root + "/bg/innerBg/unlocked";

            public const string MaxedTxt = Root + "/bg/innerBg/maxed";

            public const string ActivateBtn = UnlockedTxt + "/buttonHolder/researchActivateButton";
        }

        public static class MeteoriteResearchPreviewLoc
        {
            private const string Root = MenusLoc.Root + "/popups/MeteoriteResearchPreview";

            public const string CloseBtn = Root + "/bg/closeButton";

            public const string NameTxt = Root + "/bg/innerBg/researchName";

            // Shown only when the node's prerequisites are met (its sibling "locked" otherwise).
            public const string UnlockedRoot = Root + "/bg/innerBg/unlocked";

            public const string ResearchBtn = UnlockedRoot + "/researchButton";

            public const string CostTxt = ResearchBtn + "/cost";
        }

        public static class OracleStoreLoc
        {
            private const string Root = MenusLoc.Root + "/menus/OracleStore";

            public const string CloseBtn = Root + "/closeButton";

            private const string OraclesGiftRoot =
                Root + "/bg/submenus/valueBundles/Scroll View/Viewport/items/oraclesGift";

            // The oraclesGift container has a Button of its own that swallows clicks; the free claim
            // is this nested one.
            public const string OraclesGiftBtn = OraclesGiftRoot + "/Graphics/purchaseButton";

            public const string OraclesGiftRenewTxt = OraclesGiftRoot + "/Graphics/renewText";
        }

        /// <summary>Path of Glory (the battle pass).</summary>
        public static class BattlePassLoc
        {
            // popups/, not menus/.
            private const string Root = MenusLoc.Root + "/popups/BattlePass";

            public const string CloseBtn = Root + "/bg/closeButton";

            public const string RewardsTabBtn = Root + "/bg/submenuButtons/rewards";

            public static class RewardsLoc
            {
                public const string TrackRoot =
                    BattlePassLoc.Root + "/bg/submenus/rewards/bg/scrollView/viewport/content/layout";

                // Relative to a pathOfGloryTier (N) child of TrackRoot. Golden claims only work while
                // the pass is owned; buying it (getGoldenPassButton) is never automated.
                public const string FreeClaimBtn = "/freeBg/glowOutlineFree/rewardRoot/claimButton";

                public const string GoldenClaimBtn = "/goldenPassBg/glowOutlineGolden/rewardRoot/claimButton";
            }
        }
    }
}
