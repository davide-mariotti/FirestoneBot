using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Firebot.Tasks.Guild;
using Il2Cpp;

namespace Firebot.GameModel.Features.Guild;

/// <summary>The Personal Tree only - the Guild Tree is shared and needs officer rank.</summary>
public static class TreeOfLife
{
    // treeOfLifePersonalUpgrade (0)..(19).
    private const int PersonalUpgradeCount = 20;

    // Bought first, up to what the tree allows (TreeOfLifePlanner) - a group, not a ranking. The F2P
    // guide's Steam source gives the tree's centre as Battlecry, Miner, Firestone Finder, Raining Gold;
    // Prestigious is the game's name for Firestone Finder.
    private static readonly HashSet<PersonalUpgradeType> PriorityUpgrades = new()
    {
        PersonalUpgradeType.RainingGold, PersonalUpgradeType.Prestigious, PersonalUpgradeType.FirestoneEffect,
        PersonalUpgradeType.BattleCry, PersonalUpgradeType.Miner
    };

    /// <summary>The Personal Tree's own level, from the game's data: no screen needed, 0 until it has loaded.</summary>
    public static int PersonalLevel => (int)(GameInitialize.HandlerLoader?.ToLPersonalUpgradeHandler?.personalTreeLevel ?? 0);

    public static IEnumerator OpenPersonalTab => new GameButton(Paths.TreeOfLifeLoc.PersonalTabBtn).Click();

    public static GameButton PersonalNode(int index) => new(NodePath(index));

    /// <summary>
    ///     Every node from the game's data, in grid order. A node goes up to the tree level's allowance
    ///     (`upgradesAllowed`: 10 at tree level 10 on Steam-4, 04/10) and its own cap (300, Hero Level Up
    ///     Cost 185); an unclickable node counts as capped.
    /// </summary>
    public static List<TreeNode> PersonalNodes()
    {
        var allowed = Allowed;
        return Enumerable.Range(0, PersonalUpgradeCount).Select(i =>
        {
            var upgrade = Upgrade(i);
            return upgrade == null || !PersonalNode(i).IsClickable() ? new TreeNode(0, 0, false) : Node(upgrade, allowed);
        }).ToList();
    }

    /// <summary>
    ///     The next pick from the game's data alone, the tree closed: its name, level and cost (int.MaxValue
    ///     when every node is at its cap). Null when the data can't be read. Unlike the screen it can't tell
    ///     a locked node, so a run it opens for nothing waits the long recheck (TreeOfLifeTask).
    /// </summary>
    public static (string Name, int Level, int Cost)? NextFromGame => GameState.Get("personal tree", () =>
    {
        var upgrades = Upgrades();
        var allowed = Allowed;
        var nodes = upgrades.Select(u => Node(u, allowed)).ToList();
        return TreeOfLifePlanner.Pick(nodes) is { } i
            ? (upgrades[i].upgradeType.ToString(), nodes[i].Level, nodes[i].Cost)
            : ("-", 0, int.MaxValue);
    });

    /// <summary>Every node as the game has it, for the log: level/cap, cost and tree XP of the next level.</summary>
    public static string DescribeFromGame => GameState.GetRef("personal tree describe", () =>
    {
        var h = GameInitialize.HandlerLoader.ToLPersonalUpgradeHandler;
        var allowed = Allowed;
        return $"tree {h.personalTreeLevel:0} xp {h.personalXp:0}, allowed {allowed}: " + string.Join(" | ", Upgrades().Select(u =>
        {
            var n = Node(u, allowed);
            return $"{u.upgradeType} {n.Level}/{n.Cap}{(n.Priority ? "*" : "")} {n.Cost}t {u.nextUpgradeXp:0}xp";
        }));
    });

    private static int Allowed => (int)(GameInitialize.HandlerLoader?.ToLPersonalUpgradeHandler?.upgradesAllowed ?? 0);

    private static List<ToLPersonalUpgrade> Upgrades()
    {
        var list = new List<ToLPersonalUpgrade>();
        foreach (var kv in GameInitialize.HandlerLoader.ToLPersonalUpgradeHandler.personalUpgrades) list.Add(kv.Value);
        return list;
    }

    private static TreeNode Node(ToLPersonalUpgrade u, int allowed) => new((int)u.level.GetDecrypted(),
        Math.Min(allowed, (int)u.levelCap), PriorityUpgrades.Contains(u.upgradeType),
        (int)(GameState.Get("personal tree cost", () => u.Cost().GetDecrypted()) ?? 0));

    /// <summary>Expedition Tokens on hand ("1.107"), from the Personal tab's counter; -1 when it doesn't read.</summary>
    public static int Tokens => (int)GameState.Balance(CurrencyCode.ExpeditionToken, () => new GameText(Paths.TreeOfLifeLoc.TokenCountTxt).GetParsedDoubleAbbreviated(-1));

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
