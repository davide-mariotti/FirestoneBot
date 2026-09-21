namespace Firebot.Infrastructure;

/// <summary>Paths rooted at menusRoot (popups/screens opened over the battle view), grown as needed.</summary>
public static partial class Paths
{
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

                // "Pacchetti Speciali" ("Extreme Value Bundle") - a SEPARATE tab from
                // ValueBundleDailyBtn above, confirmed via docs/screens/Store.html
                // (grid/valueBundleButton opens submenu "extremeValueBundles"). This tab has its own
                // independent free mystery box (see ExtremeValueBundlesLoc below) that the daily-tab
                // claim never touches - root cause of the claim never firing on accounts that land on
                // this tab (2026-09-21 live diagnostic: Steam-1's badge stayed lit and
                // DailyStoreOffersTask kept re-triggering off Notifications.MysteryBox every ~8s
                // without ever clearing it, while Steam-0 - which had already bought the paid
                // extremeValueBundle - showed valueBundleDaily instead and claimed fine).
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

                // The one free slot in this tab (has a "freeText" label instead of a price) - NOT
                // the numbered valueBundle (0)/(1)/(2) slots next to it, those are real-money/premium
                // purchases and must never be auto-clicked.
                public const string FreeMysteryBoxBtn =
                    Root + "/Scroll View/Viewport/bundles/mysteryBox/Graphics/purchaseButton";

                public const string RenewTxt = Root + "/timeRenewBackground/renewText";
            }

            // "Pacchetti Speciali" tab (opened via TabsLoc.ValueBundleBtn) - confirmed via
            // docs/screens/Store.html: "extremeValueBundles/bundles" is a fixed HorizontalLayoutGroup
            // row (not a ScrollView like ValueBundleDailyLoc) holding exactly two fixed cards,
            // "mysteryBox" and "extremeValueBundle". Same claim-button shape as ValueBundleDailyLoc's
            // mysteryBox (purchaseButton + priceHolder/freeText when free, claimedBG/claimedObj once
            // claimed) but a genuinely separate GameObject/claim state from it - both must be claimed
            // independently.
            public static class ExtremeValueBundlesLoc
            {
                private const string Root = StoreLoc.Root + "/bg/submenus/extremeValueBundles/bundles";

                // The free mystery box card - NOT "extremeValueBundle" (the sibling card in this same
                // row), which is the real-money paid bundle and must never be auto-clicked, same rule
                // as ValueBundleDailyLoc's numbered valueBundle slots.
                public const string FreeMysteryBoxBtn = Root + "/mysteryBox/Graphics/purchaseButton";
            }
        }

        public static class CharacterLoc
        {
            internal const string Root = MenusLoc.Root + "/popups/Character";

            // Root path convention not independently live-verified for this screen (a newly
            // automated feature, no prior code to cross-check against) - inferred from every other
            // menu screen checked so far (Store, OracleStore) consistently using menusRoot/.../menus/<Name>.
            public const string CloseBtn = Root + "/bg/closeButton";

            public const string QuestsTabBtn = Root + "/bg/submenuButtons/quests";

            // Confirmed via a targeted UnityPy scan (Character/bg/submenuButtons/talents), including
            // its own notification bell (text/lock/notification/bell/amountTMP) - see
            // Talents.cs for the full 89-node tree behind this tab.
            public const string TalentsTabBtn = Root + "/bg/submenuButtons/talents";

            public static class QuestsLoc
            {
                private const string Root = CharacterLoc.Root + "/bg/submenus/quests";

                public const string DailyTabBtn = Root + "/bg/submenuButtons/dailyButton";

                public const string WeeklyTabBtn = Root + "/bg/submenuButtons/weeklyButton";

                public const string DailyQuestsGridRoot = Root + "/bg/submenus/dailyQuestsScroll/Viewport/grid";

                public const string WeeklyQuestsGridRoot = Root + "/bg/submenus/weeklyQuestsScroll/Viewport/grid";

                // Shared between both tabs - shows whichever tab's countdown is currently selected
                // (docs/screens/Character.html: "rinnovo delle missioni (giornaliere/settimanali)").
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

            // Confirmed via UnityPy in an earlier session (see PLAN.md's Chaos Rift open point) -
            // the only piece of that earlier scan that was ever confirmed; the real attack/shop
            // screens themselves were not found until now.
            public const string ChaosRiftBtn = Root + "/chaosRift";

            // Guessed by the same camelCase-of-the-feature-name convention as every sibling icon
            // above (treeOfLife, arcaneCrystal, awakening) - never independently confirmed via
            // UnityPy or live. Fix via TownGuild's own child dump if wrong (see
            // ForbiddenKnowledgeTask's diagnostic fallback).
            public const string ForbiddenKnowledgeBtn = Root + "/forbiddenKnowledge";
        }

        // Unlike the "menus" screens above, Expeditions lives under menuCanvas/popups - an
        // event-triggered overlay rather than a permanent hub screen.
        public static class ExpeditionsLoc
        {
            private const string Root = MenusLoc.Root + "/popups/Expeditions";

            public const string CloseBtn = Root + "/bg/closeButton";

            public const string NextRunTimeTxt = Root + "/bg/timeLeftBg/timeLeftText";

            private const string ActiveExpeditionRoot =
                Root + "/bg/expeditionsParent/activeExpeditionParent/activeExpedition";

            public const string ActiveExpedition = ActiveExpeditionRoot;

            public const string ClaimBtn = ActiveExpeditionRoot + "/claimButton";

            public const string CurrentRunTimeTxt = ActiveExpeditionRoot + "/expeditionProgressBg/timeLeftText";

            public const string StartBtn =
                Root +
                "/bg/expeditionsParent/pendingExpeditionsParent/expeditionsScroll/Viewport/grid/expeditionPending0/startButton";
        }

        public static class TownIrongardLoc
        {
            // Reverted back to "menus/TownIrongard": the previous "popups/TownIrongard" change (see
            // git history) was never actually confirmed live and turned out wrong. Live test
            // (2026-09-17, Guardian Training) showed "popups/TownIrongard/townBg/parent/magicQuarters"
            // failing to resolve at all (node doesn't exist), while the Watchdog's generic sweep -
            // which enumerates the *real* live children of "menus/" - independently found a child
            // literally named "TownIrongard" there, with a resolvable (if currently inactive)
            // closeButton. That's a live structural fact, not a guess: this hub lives under "menus/".
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

            // Opens WFMenuSelectionLoc, a hub with 2 options (campaign/arena) - confirmed via
            // UnityPy. Matches the wiki's "Arena of Kings [...] is accessible from the Battles
            // building".
            public const string BattlesBtn = Root + "/townBg/parent/battles";

            // Confirmed via UnityPy full child dump of townBg/parent (24 building icons) - missed in
            // the first pass over Hall of Heroes (only grepped the icons already mapped in this file
            // instead of dumping the live list), corrected after the user pointed out live in-game
            // that Hall of Heroes is reached through Town, not just the notification rail.
            public const string HallOfHeroesBtn = Root + "/townBg/parent/hallOfHeroes";

            // Live-confirmed, 2026-09-18: full dump of townBg/parent (21 children) shows "ship", not
            // the originally-guessed "pirateShip" - docs/screens/PirateShip.html only dumped the
            // screen itself, not the Town building icon. Note a separate "merchantShip" also exists
            // in the same list (a different feature) - "ship" is the one that actually opened
            // "menus/PirateShip" when tested.
            public const string PirateShipBtn = Root + "/townBg/parent/ship";
        }

        // Live-confirmed, 2026-09-18 (user screenshot + dump): clicking the "tavern" building
        // doesn't jump straight into the card-flip screen - it opens this intermediate choice popup
        // with two cards, real names "tavern" (the card game) and "scarabGame". A second click on
        // the right card is needed to actually enter either destination - see Town.OpenTavern /
        // Town.OpenScarabGame, which both do the building click + card click as one step.
        public static class TavernSelectionLoc
        {
            private const string Root = MenusLoc.Root + "/popups/TavernSelection";

            public const string CloseBtn = Root + "/bg/closeButton";

            private const string CardsRoot = Root + "/bg";

            public const string OpenTavernBtn = CardsRoot + "/tavern";

            public const string OpenScarabGameBtn = CardsRoot + "/scarabGame";
        }

        // The Tavern's own card-flip minigame screen, entered via Town.OpenTavern (building click +
        // "tavern" card in TavernSelection - see that class' doc comment). Scarab's Game turned out
        // to be a sibling card in that same selection popup (Town.OpenScarabGame), not something
        // reached from inside this screen - the "shop" action button once assumed here for that was
        // never confirmed live and has been removed.
        public static class TavernLoc
        {
            private const string Root = MenusLoc.Root + "/menus/Tavern";

            public const string CloseBtn = Root + "/closeButton";

            // Opens TavernMarket - "Stormy, the tavern keeper" per the wiki's Tavern Market section.
            public const string OpenMarketBtn = Root + "/helpCanvas/stormyButton";

            // Starts a round at the current quantity - costs 1 game token per draw at the default
            // "x1" multiplier (live-confirmed via user screenshot: "Play 1" costs 1, "Play 10" costs
            // 10 - linear). Doesn't deduct tokens or count towards the quest by itself: it reveals a
            // set of face-down card stacks (see CardsRoot) and a card still has to be picked to
            // actually trigger the reveal animation and complete the round (live-confirmed, 2026-09-18
            // - the token count stayed unchanged right after Play until a card was clicked).
            public const string PlayBtn = Root + "/helpCanvas/bottomUI/playButton";

            // Live-confirmed, 2026-09-18 (user screenshot): 6 identical face-down card stacks appear
            // after Play, real names "tavernCard0".."tavernCard5" - clicking any one completes the
            // round (they're interchangeable for a bundled/lucky-style draw, not separate outcomes).
            public const string CardsRoot = Root + "/helpCanvas/cardHolder";

            public const string FirstCardBtn = CardsRoot + "/tavernCard0";

            // Live-confirmed, 2026-09-18: cycles the draw-count multiplier (user screenshot: "x1" /
            // "x10" seen) - same "changeQuantity" pattern as ScarabGame/PharaohsVault. Lets
            // GamerQuestTask cover its whole daily quota (10 draws) in one click when affordable,
            // same idea as ArcaneCrystal's hit-quantity multiplier for Miner Quest.
            public const string ChangeQuantityBtn = Root + "/helpCanvas/bottomRightUI/changeQuantity";

            public const string QuantityTxt = ChangeQuantityBtn + "/text";

            // Live-confirmed, 2026-09-18: the real, populated currency counter is
            // "currencyInteraction (GameToken)" - its literal name spells out which currency it's
            // bound to. A sibling "counterInteraction" (generic/pooled, previously assumed to be this
            // counter) exists but stays inactive on this screen - reading it always returned empty.
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

            public const string CloseBtn = Root + "/bg/closeButton";

            public const string EmpowerBtn = Root + "/bg/empowerBg/empowerButton";
        }

        // Generic "this costs something, confirm?" gate - only used by the Empower flow so far.
        public static class ActionRequiredLoc
        {
            private const string Root = MenusLoc.Root + "/popups/ActionRequired";

            public const string ConfirmBtn = Root + "/bg/confirmButton";

            public const string CancelBtn = Root + "/bg/cancelButton";
        }

        // "Temple Of Eternals Prestige Complete" - dismissal popup shown right after a successful empower.
        public static class TOEPrestigeCompleteLoc
        {
            private const string Root = MenusLoc.Root + "/popups/TOEPrestigeComplete";

            public const string ConfirmBtn = Root + "/bg/confirmButton";
        }

        // Live-confirmed, 2026-09-18: the "Engineer" building doesn't jump straight into the
        // Engineer screen either - it opens this intermediate choice popup first, real names
        // "engineer" / "garage" / "trainingBase" (user screenshot: "Engineer" / "Garage" /
        // "Training base"). Same pattern as Tavern's TavernSelection. War Machines turned out to
        // live behind "garage", not behind the Engineer screen - see Town.OpenEngineer /
        // Town.OpenWarMachines, which both do the building click + card click as one step.
        public static class GarageSelectionLoc
        {
            private const string Root = MenusLoc.Root + "/popups/GarageSelection/bg";

            public const string CloseBtn = Root + "/closeButton";

            public const string OpenEngineerBtn = Root + "/engineer";

            public const string OpenGarageBtn = Root + "/garage";

            public const string OpenTrainingBaseBtn = Root + "/trainingBase";
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

            // Relative to a guardian child - GuardianLoc below.
            public const string GuardianStarsIcon = "/starsParent";

            private const string UnlockedGuardianRoot = Root + "/submenus/bg/infoSubmenu/activities/unlocked";

            public const string EnlightenmentBtn = UnlockedGuardianRoot + "/enlightenment/enlightenmentButton";

            public const string TrainBtn = UnlockedGuardianRoot + "/train/trainButton";

            public const string NextRunTimeTxt = TrainBtn + "/cooldownOn/cooldownTimeLeft";

            // Live-confirmed, 2026-09-20: 5 guardian tabs total, per the user - training (info),
            // evolution, Chaos Rift (Holy damage), rarity, skin. Real structure:
            // submenus/bg/{infoSubmenu,chaosRiftSubmenu,raritySubmenu,skinSubmenu,evolutionSubmenu}
            // (content, only the active one visible) + submenus/submenuButtons/{info,evolution,
            // chaosRift,rarity,skin} (the tab buttons themselves). Reaching MagicQuarters via Chaos
            // Rift's own "Upgrades" button lands directly on chaosRiftSubmenu already active, no tab
            // click needed - but the button is exposed below too for robustness.
            public const string SubmenusRoot = Root + "/submenus";

            public const string ChaosRiftTabBtn = SubmenusRoot + "/submenuButtons/chaosRift";

            public const string ChaosRiftSubmenuRoot = SubmenusRoot + "/bg/chaosRiftSubmenu";

            // Live-confirmed, 2026-09-20 (3 rounds of diagnostics) - the "Upgrade N [cost]" button
            // from the user's screenshot. Spends Orbs of Light (per the wiki) - resets every month,
            // so no reason to ever hold back spending it.
            public const string ChaosRiftUpgradeBtn = ChaosRiftSubmenuRoot + "/holyDamageUpgrade/bg/upgradeButton";
        }

        // Overlay shown if a still-locked guardian is clicked - separate popup, own close button.
        public static class LockedGuardianLoc
        {
            private const string Root = MenusLoc.Root + "/popups/LockedGuardian";

            public const string CloseBtn = Root + "/bg/closeButton";
        }

        // Generic one-off validation toast the game reuses for various blocked actions (e.g.
        // "You need to complete tree I first" when trying to jump to a locked Firestone Research
        // tree). Lives under popups/, sibling to the screen that triggered it - not nested inside
        // it - so closing it does not touch whatever screen (Library, TownIrongard, ...) is
        // legitimately still open underneath.
        public static class GenericMessageLoc
        {
            private const string Root = MenusLoc.Root + "/popups/GenericMessage";

            public const string CloseBtn = Root + "/bg/closeButton";
        }

        // Live-confirmed, 2026-09-18: a SEPARATE popup from GenericMessage above, specifically for
        // "You need N more <currency>..." warnings (found via TreeOfLifeTask trying to buy an upgrade
        // without enough Expedition Tokens) - initially assumed to be GenericMessage, which turned out
        // to be a different, wrong popup name entirely.
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

                // Bug found via live testing: the whole "freePickaxe" row/container has no Button
                // component at all (confirmed via UnityPy - zero components on that GameObject), so
                // this was always a silent no-op. The real click target is the nested "purchaseButton"
                // (labeled "Gratis"/free via its own "freeText" child) - same pattern as the Task 3
                // mystery box and Task 18 Scarab Game free gift.
                public const string ClaimBtn = Root + "/claimBg/purchaseButton";

                public const string QuantityTxt = Root + "/claimBg/itemBg/itemQuantity";

                public const string NextRunTimeTxt = Root + "/nextFreeObj/progressBarBg/timeLeftText";
            }
        }

        // The "Oracle" building/screen (rituals) - distinct from OracleStoreLoc below (the value-bundle
        // shop opened via the OraclesGift notification, Task 2).
        public static class OracleLoc
        {
            private const string Root = MenusLoc.Root + "/menus/Oracle";

            public const string CloseBtn = Root + "/closeButton";

            public static class RitualLoc
            {
                private const string Root = OracleLoc.Root + "/submenus/bg/ritualSubmenu";

                public const string Rituals = Root + "/ritualsGrid";

                // Relative to a ritual child (Rituals.Claim/Start pass a child as GameButton's parent) -
                // confirmed via UnityPy: claimButton/startButton are direct children of each
                // oracleRitualInteraction (N), not nested under ritualProgressBg. An earlier fix
                // attempt wrongly made this an absolute Root-prefixed path, which - combined with
                // being used as a relative suffix against a child parent - built a garbage
                // double-nested path; caught by checking the real structure and how Rituals.Claim()
                // actually calls this (GameButton(ClaimBtn, child), not standalone).
                public const string ClaimBtn = "/claimButton";

                public const string CurrentRunTimeTxt = "/ritualProgressBg/timeLeftText";

                public const string StartBtn = "/startButton";

                // Confirmed via UnityPy: "timeLeft", not "timeLeftText" (that name only exists one
                // level deeper, per-ritual-slot under ritualProgressBg - see CurrentRunTimeTxt above).
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

                public const string StartBtn = "/startExperiment";

                // Relative to a resource slot (interpolated into "/{Slot}{resource}/{ClaimBtn}" in
                // Experiments.Claim()) - confirmed via UnityPy: claimButton is a direct child of each
                // alchExperimentSlot(N), a sibling of progressBarBg, not nested under it. An earlier
                // fix attempt wrongly nested it under progressBarBg AND made it Root-absolute, which
                // would build a garbage path once interpolated - same mistake as Oracle Rituals'
                // ClaimBtn, caught the same way (checking the real structure and how Claim() actually
                // builds the path).
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

            // Library has two tabs (meteoriteResearch, firestoneResearch) and firestoneResearch is
            // NOT selected by default when the screen opens (docs/screens/Library.html line 95) -
            // must be clicked explicitly, same as every other multi-tab screen in this codebase.
            public const string FirestoneResearchTabBtn = Root + "/submenuButtons/firestoneResearch";

            public const string MeteoriteResearchTabBtn = Root + "/submenuButtons/meteoriteResearch";

            public static class ResearchPanelLoc
            {
                public const string Root = LibraryLoc.Root + "/submenus/firestoneResearch/researchPanel";

                public const string SelectResearchTable = Root + "/selectResearchTable";

                public const string UnlockSlotBtn = Root + "/unlockResearchSlot/confirmButton";

                // Relative to a research-table child (ResearchPanel.Claim passes a child as
                // GameButton's parent) - a prior fix attempt wrongly made this Root-absolute, same
                // mistake as Oracle Rituals/Experiments' ClaimBtn above, caught the same way.
                public const string ClaimBtn = "/container/claimButton";

                public const string NextRunTimeTxt = "/container/researchInfo/progressBarBg/timeLeftText";

                public const string SpeedupBtn = "/container/speedUpButton";

                public const string SpeedupFinishDesc = SpeedupBtn + "/finishDesc";
            }

            public static class NodeLoc
            {
                private const string SubmenuRoot = LibraryLoc.Root + "/submenus/firestoneResearch";

                public const string Root = SubmenuRoot + "/researchScrollView/viewport/content/submenus";

                public const string Glow = "/glow";

                public const string ProgressBar = "/progressBarBg";

                public const string CompletedTxt = "/genericText";

                // 3 trees total, one visible/active at a time - same carousel pattern as
                // MeteoriteResearchLoc below, confirmed against the raw prefab dump (both submenus
                // have their own navigation/goBackTree+goForthTree, not just a single scroll view).
                public const string NextTreeBtn = SubmenuRoot + "/navigation/goForthTree";

                public const string PreviousTreeBtn = SubmenuRoot + "/navigation/goBackTree";
            }

            // Meteorite Research tab: 5 trees x 13 nodes (research0..12), carousel navigation like
            // NodeLoc above. Unlike firestoneResearch nodes (which show level/progress/time inline),
            // these only show an icon + level - clicking one always opens MeteoriteResearchPreviewLoc
            // to see cost/unlock state, confirmed via a fresh UnityPy scan (docs/screens/Library.html
            // didn't capture this popup, same static-analysis gap hit before with Empower's popups).
            public static class MeteoriteResearchLoc
            {
                private const string SubmenuRoot = LibraryLoc.Root + "/submenus/meteoriteResearch";

                private const string NavigationRoot = SubmenuRoot + "/navigation";

                public const string NextTreeBtn = NavigationRoot + "/goForthTree";

                public const string PreviousTreeBtn = NavigationRoot + "/goBackTree";

                // Each tree's children are researchPath0..12 (13 decorative connector lines) FOLLOWED
                // by research0..12 (13 real node buttons) - confirmed via UnityPy child-order dump, so
                // GetChild(13 + index) reaches research{index} for index 0..12.
                public const string TreesRoot = SubmenuRoot + "/submenus";
            }
        }

        // Popup shown when a research node is clicked - lives under menuCanvas/popups, not menus/Library.
        public static class FirestoneResearchPreviewLoc
        {
            private const string Root = MenusLoc.Root + "/popups/FirestoneResearchPreview";

            public const string CloseBtn = Root + "/bg/closeButton";

            // Confirmed via UnityPy. Used to prioritize "Raining Gold" over other unlocked talents,
            // per the user (matches an external tips guide's advice - see FirestoneResearchTask).
            public const string NameTxt = Root + "/bg/innerBg/researchName";

            public const string LevelTxt = Root + "/bg/innerBg/researchLevelText";

            public const string UnlockedTxt = Root + "/bg/innerBg/unlocked";

            public const string MaxedTxt = Root + "/bg/innerBg/maxed";

            public const string RealTimeTxt = UnlockedTxt + "/researchPending/realTime";

            public const string ActivateBtn = UnlockedTxt + "/buttonHolder/researchActivateButton";
        }

        // Popup shown when a meteorite research node is clicked. Found via a fresh UnityPy scan (not
        // in docs/screens/Library.html, which only captured the node grid itself) - no prior precedent
        // either (never implemented this feature at all). Root path inferred from the same
        // popups/<Name> convention every other popup in this file already uses and that already-
        // proven, live-tested code confirms for FirestoneResearchPreview/EmpowerPopup/etc - flag for
        // live verification since this specific instance has no direct cross-check.
        public static class MeteoriteResearchPreviewLoc
        {
            private const string Root = MenusLoc.Root + "/popups/MeteoriteResearchPreview";

            public const string CloseBtn = Root + "/bg/closeButton";

            // Confirmed via UnityPy. Used to prioritize "Raining Gold" over other unlocked talents,
            // per the user (matches an external tips guide's advice - see MeteoriteResearchTask).
            public const string NameTxt = Root + "/bg/innerBg/researchName";

            public const string LevelTxt = Root + "/bg/innerBg/level";

            // Shown only when the node's prerequisites are met (sibling to "locked", which shows
            // requirement info instead when they're not) - same on/off pattern as
            // FirestoneResearchPreviewLoc.UnlockedTxt above.
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

            // Fixed: the container "oraclesGift" itself has its own Button component (a select/
            // preview interaction), which silently ate every click intended for the actual claim -
            // same "clicked the wrong nested Button" shape as Free Pickaxes, confirmed via UnityPy
            // script-name resolution (both container and purchaseButton genuinely have their own
            // UnityEngine.UI.Button). The real free-claim target is the nested "purchaseButton".
            public const string OraclesGiftBtn = OraclesGiftRoot + "/Graphics/purchaseButton";

            // Sibling of purchaseButton under "Graphics", not nested inside it - kept independent of
            // OraclesGiftBtn so a future change to one doesn't silently break the other.
            public const string OraclesGiftRenewTxt = OraclesGiftRoot + "/Graphics/renewText";
        }

        // Battle Pass ("Path of Glory"). Never automated before.
        // Paths confirmed via a fresh UnityPy scan of the live game assets (docs/screens/BattlePass.html
        // abbreviated some intermediate decorative nodes - "goldenPassBg/.../rewardRoot", the exact
        // "..." was found by dumping a real pathOfGloryTier node directly).
        public static class BattlePassLoc
        {
            // Confirmed live, 2026-09-17 (PathOfGloryTask diagnostic): "menus/BattlePass" doesn't
            // exist at all, "popups/BattlePass" does (activeSelf=True/activeInHierarchy=True) - same
            // wrong-root pattern as TownIrongard/Character/SystemMail found earlier the same day.
            private const string Root = MenusLoc.Root + "/popups/BattlePass";

            public const string CloseBtn = Root + "/bg/closeButton";

            // Confirmed active by default on open (docs/screens/BattlePass.html), but clicked
            // unconditionally anyway per the usual "never trust the default tab" rule.
            public const string RewardsTabBtn = Root + "/bg/submenuButtons/rewards";

            public static class RewardsLoc
            {
                public const string TrackRoot =
                    BattlePassLoc.Root + "/bg/submenus/rewards/bg/scrollView/viewport/content/layout";

                // Both relative to a pathOfGloryTier (N) child. Free needs no ownership; Golden
                // requires owning the premium pass (purchased separately via getGoldenPassButton,
                // never auto-clicked) - but claiming an already-unlocked Golden reward isn't itself a
                // purchase, and the button is inactive/non-interactable (safe no-op) when locked or
                // not yet reached, same convention as every other claim button in this codebase.
                public const string FreeClaimBtn = "/freeBg/glowOutlineFree/rewardRoot/claimButton";

                public const string GoldenClaimBtn = "/goldenPassBg/glowOutlineGolden/rewardRoot/claimButton";
            }
        }
    }
}
