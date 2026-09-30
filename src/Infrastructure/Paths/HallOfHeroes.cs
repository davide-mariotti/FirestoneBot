namespace Firebot.Infrastructure;

/// <summary>Hall of Heroes: gear tier unlocks and gear/jewel enchanting. Verified live on 2026-09-29.</summary>
public static partial class Paths
{
    public static class HallOfHeroesLoc
    {
        private const string Root = MenusLoc.Root + "/menus/HallOfHeroes";

        public const string CloseBtn = Root + "/closeButton";

        // One active cell per owned hero (10 on Steam-0, the same heroes goForthHero walks, in the same
        // order), named "hero (N)" out of order or "heroSquare(Clone)", plus inactive spare clones and a
        // trailing "allHeroesButton". Selecting a hero doesn't reorder them.
        public const string HeroGridRoot = Root + "/characterListScrollView/Viewport/heroGrid";

        public const string HeroNameTxt = Root + "/base/characterPreview/standardLevelProgress/bg/name";

        // Both counters show on every tab, '.'-grouped ("3.010").
        public const string VoidCrystalsTxt = Root + "/counters/currencyInteraction (VoidCrystal)/quantity";

        public const string EtherealShardsTxt = Root + "/counters/currencyInteraction (EtherealShard)/quantity";

        public const string GearTabBtn = Root + "/submenus/submenuButtons/gear";

        public const string EnchantingTabBtn = Root + "/submenus/submenuButtons/enchanting";

        // Separate prefabs, instantiated under submenus/bg the first time their tab opens.
        public static class GearSubmenuLoc
        {
            private const string Root = HallOfHeroesLoc.Root + "/submenus/bg/gearSubmenu";

            public const string GalleryViewBtn = Root + "/galleryBtn";

            // The hero's gear power ("74.950"); galleryView/powerBg/Image/power is only the "Power" label.
            public const string GearPowerTxt = Root + "/galleryView/gear/unlocked/powerGearTMP";

            private const string GalleryGearRoot = Root + "/galleryView/gear/unlocked/itemList";

            public const string Tier2LockedRoot = GalleryGearRoot + "/tier2Locked";

            public const string Tier3LockedRoot = GalleryGearRoot + "/tier3Locked";

            public const string UnlockTier2Btn = Tier2LockedRoot + "/unlockTier2Button";

            public const string UnlockTier3Btn = Tier3LockedRoot + "/unlockTier3Button";

            // No unlock button: jewel tier 2 unlocks by itself.
            public const string JewelTier2LockedRoot = Root + "/galleryView/jewels/unlocked/itemList/tier2Locked";
        }

        // Opened by unlockTierNButton: the unlock only happens on its confirmButton. Seen live on
        // 2026-09-30: powerLevelReqText '<color=#0FE607>1300</color>' (6600 for tier 3), cost 480
        // Meteorites for tier 2 and 720 for tier 3, currencyIcon sprite 'meteorite64'.
        public static class GearTierUnlockLoc
        {
            private const string Root = MenusLoc.Root + "/popups/GearTierUnlock/bg";

            public const string PowerRequirementTxt = Root + "/powerLevelReqText";

            public const string ConfirmBtn = Root + "/confirmButton";

            public const string CostTxt = ConfirmBtn + "/costText";

            public const string CurrencyIcon = ConfirmBtn + "/currencyIcon";

            public const string CloseBtn = Root + "/closeButton";
        }

        public static class EnchantingSubmenuLoc
        {
            private const string Root = HallOfHeroesLoc.Root + "/submenus/bg/enchantingSubmenu";

            public const string GearCategoryTabBtn = Root + "/categoryButtons/gear";

            public const string JewelsCategoryTabBtn = Root + "/categoryButtons/jewels";

            // Rows are renamed after their slot at runtime ("Weapon".."Relic", "Ankh".."Trinket"), in
            // the order EnchantPlanner uses; the effect descriptions confirm which is which.
            public const string GearRowsRoot = Root + "/categories/gearCategory/gearScrollView/viewport/content";

            public const string JewelRowsRoot = Root + "/categories/jewelsCategory/jewelScrollView/viewport/content";

            // Relative to a row.
            public const string RowEnchantBtn = "mainBg/enchantItem";

            // Plain or '.'-grouped ("240", "1.920").
            public const string RowCostTxt = "mainBg/enchantItem/costText";

            // Hidden at level 0.
            public const string GearRowLevelBg = "mainBg/gearItem/enchantLevelBg";

            public const string JewelRowLevelBg = "mainBg/jewelItem/enchantLevelBg";
        }
    }
}
