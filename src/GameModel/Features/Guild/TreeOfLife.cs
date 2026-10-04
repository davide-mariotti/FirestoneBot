using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Firebot.Tasks.Guild;
using Il2Cpp;

namespace Firebot.GameModel.Features.Guild;

/// <summary>The Personal Tree only - the Guild Tree is shared and needs officer rank.</summary>
public static class TreeOfLife
{
    // treeOfLifePersonalUpgrade (0)..(19).
    private const int PersonalUpgradeCount = 20;

    // Bought first at every step of 5 levels (TreeOfLifePlanner) - a group, not a ranking. The F2P
    // guide's Steam source gives the tree's centre as Battlecry, Miner, Firestone Finder, Raining Gold;
    // Prestigious is the game's name for Firestone Finder.
    private static readonly HashSet<PersonalUpgradeType> PriorityUpgrades = new()
    {
        PersonalUpgradeType.RainingGold, PersonalUpgradeType.Prestigious, PersonalUpgradeType.FirestoneEffect,
        PersonalUpgradeType.BattleCry, PersonalUpgradeType.Miner
    };

    public static IEnumerator OpenPersonalTab => new GameButton(Paths.TreeOfLifeLoc.PersonalTabBtn).Click();

    public static GameButton PersonalNode(int index) => new(NodePath(index));

    /// <summary>
    ///     Every node from the game's data, in grid order. A node goes up to the tree level's allowance
    ///     (`upgradesAllowed`: 10 at tree level 10 on Steam-4, 04/10) and its own cap (300, Hero Level Up
    ///     Cost 185); an unclickable node counts as capped.
    /// </summary>
    public static List<TreeNode> PersonalNodes()
    {
        var allowed = (int)(GameInitialize.HandlerLoader?.ToLPersonalUpgradeHandler?.upgradesAllowed ?? 0);
        return Enumerable.Range(0, PersonalUpgradeCount).Select(i =>
        {
            var upgrade = Upgrade(i);
            return upgrade == null || !PersonalNode(i).IsClickable()
                ? new TreeNode(0, 0, false)
                : new TreeNode((int)upgrade.level.GetDecrypted(), Math.Min(allowed, (int)upgrade.levelCap), PriorityUpgrades.Contains(upgrade.upgradeType));
        }).ToList();
    }

    public static int PersonalNodeLevel(int index) => (int?)Upgrade(index)?.level.GetDecrypted() ?? -1;

    public static string PersonalUpgradeName(int index) => Upgrade(index)?.upgradeType.ToString() ?? $"#{index}";

    public static IEnumerator Close => new GameButton(Paths.TreeOfLifeLoc.CloseBtn).Click();

    /// <summary>
    ///     Buys in the preview a node click opens, then closes it. Running out of tokens shows the
    ///     CurrencyMissing popup rather than disabling the button - callers check for it.
    /// </summary>
    public static IEnumerator ConfirmPurchase()
    {
        var buyBtn = new GameButton(Paths.TreeOfLifeLoc.PersonalUpgradePreviewBuyBtn);
        if (buyBtn.IsClickable()) yield return buyBtn.Click();

        yield return new GameButton(Paths.TreeOfLifeLoc.PersonalUpgradePreviewCloseBtn).Click();
    }

    private static ToLPersonalUpgrade Upgrade(int index) =>
        GameElement.FindTransform(NodePath(index))?.GetComponent<ToLPersonalUpgradeInteraction>()?.upgrade;

    private static string NodePath(int index) =>
        $"{Paths.TreeOfLifeLoc.PersonalNodeRoot}/treeOfLifePersonalUpgrade ({index})";
}
