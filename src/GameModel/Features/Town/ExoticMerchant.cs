using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Firebot.Utilities;
using Il2CppTMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Firebot.GameModel.Features.Town;

public static class ExoticMerchant
{
    public static IEnumerator OpenSellItemsTab => new GameButton(Paths.ExoticMerchantLoc.SellItemsTabBtn).Click();

    public static IEnumerator OpenUpgradesTab => new GameButton(Paths.ExoticMerchantLoc.UpgradesTabBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.ExoticMerchantLoc.CloseBtn).Click();

    public static GameElement UpgradesList => new(Paths.ExoticMerchantLoc.UpgradesLoc.UpgradesListRoot);

    /// <summary>The Sell tab's items in grid order, by their visible name ("Scroll of Health").</summary>
    public static List<(string Name, int Quantity)> SellItems()
    {
        var items = new List<(string, int)>();
        foreach (var cell in SellCells())
            items.Add((ItemName(cell), (int)StringUtils.ParseAbbreviated(Text(cell, Paths.ExoticMerchantLoc.SellLoc.ItemQuantityTxt))));
        return items;
    }

    /// <summary>Sells one of the named item. False when it isn't on the grid or can't be sold now.</summary>
    public static bool TrySellOne(string itemName)
    {
        foreach (var cell in SellCells())
        {
            if (ItemName(cell) != itemName) continue;

            var button = cell.Find(Paths.ExoticMerchantLoc.SellLoc.SellBtn)?.GetComponent<Button>();
            if (button == null || !button.gameObject.activeInHierarchy || !button.enabled || !button.interactable)
                return false;

            button.onClick.Invoke();
            return true;
        }

        return false;
    }

    private static IEnumerable<Transform> SellCells()
    {
        var grid = GameElement.FindTransform(Paths.ExoticMerchantLoc.SellLoc.ProductGridRoot);
        if (grid == null) yield break;

        for (var i = 0; i < grid.childCount; i++)
        {
            var cell = grid.GetChild(i);
            if (cell.gameObject.activeInHierarchy) yield return cell;
        }
    }

    // A typographic apostrophe would otherwise miss "Midas' Touch".
    private static string ItemName(Transform cell) =>
        Text(cell, Paths.ExoticMerchantLoc.SellLoc.ItemNameTxt).Replace('’', '\'');

    private static string Text(Transform cell, string path) => cell.Find(path)?.GetComponent<TMP_Text>()?.text ?? "";
}
