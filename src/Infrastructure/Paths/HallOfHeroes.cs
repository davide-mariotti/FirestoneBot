namespace Firebot.Infrastructure;

/// <summary>
///     Hero roster/gear management screen. Confirmed via UnityPy (screen root literally named
///     "HallOfHeroes", not renamed) - root path convention inferred from every other menus/&lt;Name&gt;
///     screen in this file, same flagged-assumption situation as CharacterLoc.
///     Reached via Town -> hallOfHeroes building icon (TownIrongardLoc.HallOfHeroesBtn) - an earlier
///     UnityPy pass missed this building and wrongly concluded the notification rail icon
///     (BattleLoc.NotificationsLoc.HallOfHeroes, kept as an opportunistic fast path) was the only
///     way in, corrected after the user pointed it out live.
/// </summary>
public static partial class Paths
{
    public static class HallOfHeroesLoc
    {
        private const string Root = MenusLoc.Root + "/menus/HallOfHeroes";

        public const string CloseBtn = Root + "/closeButton";

        // Pooled scroll list - children are "hero (0)", "hero (1)", ... plus a trailing
        // "allHeroesButton" (opens a separate full-roster screen, not a hero) that must be filtered
        // out by name, confirmed via UnityPy.
        public const string HeroGridRoot = Root + "/characterListScrollView/Viewport/heroGrid";

        public const string GearTabBtn = Root + "/submenus/submenuButtons/gear";

        public const string EnchantingTabBtn = Root + "/submenus/submenuButtons/enchanting";

        // Tier-unlock UI (browse gear + unlock T2/T3). Confirmed via UnityPy: the screen has two view
        // modes (galleryBtn/listViewBtn); "galleryView" is the one active by default, "listView" is
        // inactive - the unlockTier2Button/unlockTier3Button pair only exists under galleryView.
        public static class GearSubmenuLoc
        {
            private const string Root = HallOfHeroesLoc.Root + "/submenus/bg/gearSubmenu";

            public const string GalleryViewBtn = Root + "/galleryBtn";

            private const string GalleryGearRoot = Root + "/galleryView/gear/unlocked/itemList";

            public const string UnlockTier2Btn = GalleryGearRoot + "/tier2Locked/unlockTier2Button";

            public const string UnlockTier3Btn = GalleryGearRoot + "/tier3Locked/unlockTier3Button";
        }

        // Enchant-action UI, separate from GearSubmenuLoc above (confirmed via UnityPy: a sibling
        // "enchantingSubmenu", not nested inside gearSubmenu). gearCategory/content holds exactly 8
        // children "gear (0)".."gear (7)" with no interspersed headers (unlike the tier-unlock grid),
        // so GetChild(index) maps directly to slot index - order confirmed against the Gear wiki's
        // tier table: 0=Weapon,1=Chest,2=Boots (T1), 3=Wrist,4=Shoulder,5=Belt (T2), 6=Ring,7=Relic (T3).
        public static class EnchantingSubmenuLoc
        {
            private const string Root = HallOfHeroesLoc.Root + "/submenus/bg/enchantingSubmenu";

            public const string GearCategoryTabBtn = Root + "/categoryButtons/gear";

            public const string GearGridRoot = Root + "/categories/gearCategory/gearScrollView/viewport/content";

            // Jewels wiki: 2 tiers, 3 slots each (Ankh/Rune/Idol T1, Talisman/Necklace/Trinket T2) -
            // jewel (0)-(5) confirmed via UnityPy, same "no interspersed headers" grid shape as gear.
            public const string JewelsCategoryTabBtn = Root + "/categoryButtons/jewels";

            public const string JewelGridRoot = Root + "/categories/jewelsCategory/jewelScrollView/viewport/content";
        }
    }
}
