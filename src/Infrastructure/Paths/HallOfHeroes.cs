namespace Firebot.Infrastructure;

/// <summary>Hall of Heroes: gear tier unlocks and gear/jewel enchanting. Not verified live yet.</summary>
public static partial class Paths
{
    public static class HallOfHeroesLoc
    {
        private const string Root = MenusLoc.Root + "/menus/HallOfHeroes";

        public const string CloseBtn = Root + "/closeButton";

        // Children are "hero (0)", "hero (1)", ... plus a trailing "allHeroesButton" that isn't a hero.
        public const string HeroGridRoot = Root + "/characterListScrollView/Viewport/heroGrid";

        public const string GearTabBtn = Root + "/submenus/submenuButtons/gear";

        public const string EnchantingTabBtn = Root + "/submenus/submenuButtons/enchanting";

        // The tier unlock buttons exist only in the gallery view, not the list view.
        public static class GearSubmenuLoc
        {
            private const string Root = HallOfHeroesLoc.Root + "/submenus/bg/gearSubmenu";

            public const string GalleryViewBtn = Root + "/galleryBtn";

            private const string GalleryGearRoot = Root + "/galleryView/gear/unlocked/itemList";

            public const string UnlockTier2Btn = GalleryGearRoot + "/tier2Locked/unlockTier2Button";

            public const string UnlockTier3Btn = GalleryGearRoot + "/tier3Locked/unlockTier3Button";
        }

        public static class EnchantingSubmenuLoc
        {
            private const string Root = HallOfHeroesLoc.Root + "/submenus/bg/enchantingSubmenu";

            public const string GearCategoryTabBtn = Root + "/categoryButtons/gear";

            // Exactly "gear (0)".."gear (7)", so a child index is the slot index: 0 Weapon, 1 Chest,
            // 2 Boots (tier 1), 3 Wrist, 4 Shoulder, 5 Belt (tier 2), 6 Ring, 7 Relic (tier 3). The
            // order comes from the Gear wiki's table, not from a live check.
            public const string GearGridRoot = Root + "/categories/gearCategory/gearScrollView/viewport/content";

            public const string JewelsCategoryTabBtn = Root + "/categoryButtons/jewels";

            // "jewel (0)".."jewel (5)": Ankh, Rune, Idol (tier 1), Talisman, Necklace, Trinket (tier 2).
            public const string JewelGridRoot = Root + "/categories/jewelsCategory/jewelScrollView/viewport/content";
        }
    }
}
