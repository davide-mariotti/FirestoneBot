using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Character;

public static class HallOfHeroes
{
    public static IEnumerator Close => new GameButton(Paths.HallOfHeroesLoc.CloseBtn).Click();

    private static GameElement HeroGrid => new(Paths.HallOfHeroesLoc.HeroGridRoot);

    /// <summary>Every real hero slot - "allHeroesButton" (trailing sibling, opens a different screen
    /// entirely) is filtered out by name, confirmed via UnityPy.</summary>
    public static GameElement[] Heroes => HeroGrid.GetChildren().Where(h => h.Name.StartsWith("hero (")).ToArray();

    public static IEnumerator SelectHero(GameElement hero) => new GameButton(parent: hero).Click();

    public static IEnumerator OpenGearTab => new GameButton(Paths.HallOfHeroesLoc.GearTabBtn).Click();

    public static IEnumerator OpenEnchantingTab => new GameButton(Paths.HallOfHeroesLoc.EnchantingTabBtn).Click();

    public static class GearTierUnlock
    {
        public static IEnumerator OpenGalleryView =>
            new GameButton(Paths.HallOfHeroesLoc.GearSubmenuLoc.GalleryViewBtn).Click();

        public static GameButton UnlockTier2Btn => new(Paths.HallOfHeroesLoc.GearSubmenuLoc.UnlockTier2Btn);

        public static GameButton UnlockTier3Btn => new(Paths.HallOfHeroesLoc.GearSubmenuLoc.UnlockTier3Btn);
    }

    public static class GearEnchanting
    {
        public static IEnumerator OpenGearCategory =>
            new GameButton(Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.GearCategoryTabBtn).Click();

        private static GameElement GearGrid => new(Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.GearGridRoot);

        // T2 (Wrist/Shoulder/Belt, indices 3-5) and T3 (Ring/Relic, indices 6-7) give an all-heroes
        // bonus per the Gear wiki's Bonuses table, so every hero gets these tried first regardless of
        // party status.
        public static readonly int[] AlwaysEnchantSlots = { 3, 4, 5, 6, 7 };

        // T1 (Weapon/Chest/Boots) only benefits the hero wearing it, so it's only worth spending
        // Void Crystals on for heroes actually in the active formation (Party.ActivePartyIndices) -
        // per the user's explicit choice ("squadra attuale, dinamico").
        public static readonly int[] ActivePartyOnlyGearSlots = { 0, 1, 2 };

        public static GameButton SlotButton(int index) => new(parent: GearGrid.GetChild(index));
    }

    public static class JewelEnchanting
    {
        public static IEnumerator OpenJewelsCategory =>
            new GameButton(Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.JewelsCategoryTabBtn).Click();

        private static GameElement JewelGrid => new(Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.JewelGridRoot);

        // All 6 slots (Ankh/Rune/Idol T1, Talisman/Necklace/Trinket T2), every hero - unlike Void
        // Crystals, Ethereal Shards have no other use per the Jewels wiki, so there's no opportunity
        // cost to weigh by scoping to which heroes currently crew a War Machine (the wiki's stated
        // condition for a jewel's bonus to actually apply). Revisit with crew-based targeting if that
        // turns out to matter in practice - would need the War Machine crew screen, not investigated.
        public static readonly int[] AllSlots = { 0, 1, 2, 3, 4, 5 };

        public static GameButton SlotButton(int index) => new(parent: JewelGrid.GetChild(index));
    }
}
