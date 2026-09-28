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

        // Tier 2 (Wrist/Shoulder/Belt) and tier 3 (Ring/Relic) boost every hero, so every hero gets
        // them. Enchanting drains each slot before moving on, so this order decides who gets the
        // Void Crystals - and the F2P guide wants the Ring first, not fourth. Fix before enabling
        // HallOfHeroesTask (and confirm the slot indices live first, see GearGridRoot).
        public static readonly int[] AlwaysEnchantSlots = { 3, 4, 5, 6, 7 };

        // Tier 1 (Weapon/Chest/Boots) only helps the hero wearing it: active formation only.
        public static readonly int[] ActivePartyOnlyGearSlots = { 0, 1, 2 };

        public static GameButton SlotButton(int index) => new(parent: GearGrid.GetChild(index));
    }

    public static class JewelEnchanting
    {
        public static IEnumerator OpenJewelsCategory =>
            new GameButton(Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.JewelsCategoryTabBtn).Click();

        private static GameElement JewelGrid => new(Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.JewelGridRoot);

        // Every slot on every hero: Ethereal Shards have no other use, so there's nothing to save them for.
        public static readonly int[] AllSlots = { 0, 1, 2, 3, 4, 5 };

        public static GameButton SlotButton(int index) => new(parent: JewelGrid.GetChild(index));
    }
}
