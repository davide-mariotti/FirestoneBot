using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;
using UnityEngine.UI;

namespace Firebot.GameModel.Features.Character;

/// <summary>
///     Reads and clicks the live Hall of Heroes. What to enchant is decided by
///     Tasks.Character.EnchantPlanner; this only knows where things are.
/// </summary>
public static class HallOfHeroes
{
    public static IEnumerator Close => new GameButton(Paths.HallOfHeroesLoc.CloseBtn).Click();

    public static bool IsOpen => IsShown(Paths.HallOfHeroesLoc.CloseBtn);

    // Checked without GameElement.IsVisible, which logs a [FAILED] line for what is here a normal state
    // (an empty slot's row, a level-0 badge, an unlocked tier's lock panel).
    private static bool IsShown(string path) =>
        GameElement.FindTransform(path) is { } t && t.gameObject.activeInHierarchy;

    // Raw transforms: the clone cells all share one name, so a path would always find the first, inactive, one.
    private static List<Transform> HeroCells()
    {
        var cells = new List<Transform>();
        var grid = GameElement.FindTransform(Paths.HallOfHeroesLoc.HeroGridRoot);
        if (grid == null) return cells;

        for (var i = 0; i < grid.childCount; i++)
        {
            var cell = grid.GetChild(i);
            if (cell.gameObject.activeInHierarchy && cell.name != "allHeroesButton") cells.Add(cell);
        }

        return cells;
    }

    public static int HeroCount => HeroCells().Count;

    /// <summary>Selects the index-th hero of the grid; false when there's no such cell to click.</summary>
    public static bool TrySelectHero(int index)
    {
        var cells = HeroCells();
        var button = index < cells.Count ? cells[index].GetComponent<Button>() : null;
        if (button == null || !button.interactable) return false;

        button.onClick.Invoke();
        return true;
    }

    public static string HeroName => new GameText(Paths.HallOfHeroesLoc.HeroNameTxt).GetParsedText().Trim();

    public static GameText VoidCrystalsTxt => new(Paths.HallOfHeroesLoc.VoidCrystalsTxt);

    public static GameText EtherealShardsTxt => new(Paths.HallOfHeroesLoc.EtherealShardsTxt);

    public static class Gallery
    {
        public static IEnumerator Open()
        {
            yield return new GameButton(Paths.HallOfHeroesLoc.GearTabBtn).Click();
            yield return new GameButton(Paths.HallOfHeroesLoc.GearSubmenuLoc.GalleryViewBtn).Click();
        }

        public static GameText GearPowerTxt => new(Paths.HallOfHeroesLoc.GearSubmenuLoc.GearPowerTxt);

        // Their desc only says "Tier N locked"; the gear power needed is in TierUnlockPopup.
        public static bool IsGearTierLocked(int tier) => IsShown(tier == 2
            ? Paths.HallOfHeroesLoc.GearSubmenuLoc.Tier2LockedRoot
            : Paths.HallOfHeroesLoc.GearSubmenuLoc.Tier3LockedRoot);

        public static GameButton UnlockGearTierBtn(int tier) => new(tier == 2
            ? Paths.HallOfHeroesLoc.GearSubmenuLoc.UnlockTier2Btn
            : Paths.HallOfHeroesLoc.GearSubmenuLoc.UnlockTier3Btn);

        public static string GearTierLockedText(int tier) => new GameText((tier == 2
            ? Paths.HallOfHeroesLoc.GearSubmenuLoc.Tier2LockedRoot
            : Paths.HallOfHeroesLoc.GearSubmenuLoc.Tier3LockedRoot) + "/desc").GetParsedText();

        public static bool IsJewelTier2Locked => IsShown(Paths.HallOfHeroesLoc.GearSubmenuLoc.JewelTier2LockedRoot);
    }

    /// <summary>The confirmation the tier unlock buttons open.</summary>
    public static class TierUnlockPopup
    {
        public static bool IsOpen => IsShown(Paths.HallOfHeroesLoc.GearTierUnlockLoc.ConfirmBtn);

        public static string PowerRequirement =>
            new GameText(Paths.HallOfHeroesLoc.GearTierUnlockLoc.PowerRequirementTxt).GetParsedText();

        public static string Cost => new GameText(Paths.HallOfHeroesLoc.GearTierUnlockLoc.CostTxt).GetParsedText();

        /// <summary>The sprite on the confirm button: what the unlock is paid in.</summary>
        public static string CurrencyIconName => IconSprite.NameAt(Paths.HallOfHeroesLoc.GearTierUnlockLoc.CurrencyIcon);

        public static GameButton ConfirmBtn => new(Paths.HallOfHeroesLoc.GearTierUnlockLoc.ConfirmBtn);

        public static IEnumerator Close => new GameButton(Paths.HallOfHeroesLoc.GearTierUnlockLoc.CloseBtn).Click();
    }

    /// <summary>Enchant rows, addressed by slot name: the game reorders them (enchantable ones first).</summary>
    public static class Enchanting
    {
        public static IEnumerator Open(bool jewels)
        {
            yield return new GameButton(Paths.HallOfHeroesLoc.EnchantingTabBtn).Click();
            yield return new GameButton(jewels
                ? Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.JewelsCategoryTabBtn
                : Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.GearCategoryTabBtn).Click();
        }

        private static string Row(bool jewels, string slotName) => (jewels
            ? Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.JewelRowsRoot
            : Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.GearRowsRoot) + "/" + slotName;

        /// <summary>An inactive row is an empty slot (or, presumably, a locked tier).</summary>
        public static bool IsRowShown(bool jewels, string slotName) => IsShown(Row(jewels, slotName));

        /// <summary>"This enchanting requires higher rarity item.": capped until the item is replaced.</summary>
        public static bool NeedsHigherRarity(bool jewels, string slotName) => IsShown(Row(jewels, slotName) + "/extraInfo");

        /// <summary>The row's enchant level: 0 while its badge is hidden, -1 when the badge isn't a number.</summary>
        public static int Level(bool jewels, string slotName, out string text)
        {
            var badge = Row(jewels, slotName) + "/" + (jewels
                ? Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.JewelRowLevelBg
                : Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.GearRowLevelBg);

            text = "";
            if (!IsShown(badge)) return 0;

            text = new GameText(badge + "/enchantLevel").GetParsedText();
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) ? level : -1;
        }

        public static GameText CostTxt(bool jewels, string slotName) =>
            new(Row(jewels, slotName) + "/" + Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.RowCostTxt);

        // Not interactable both when the balance is short and when the item needs a higher rarity.
        public static GameButton EnchantBtn(bool jewels, string slotName) =>
            new(Row(jewels, slotName) + "/" + Paths.HallOfHeroesLoc.EnchantingSubmenuLoc.RowEnchantBtn);
    }
}
