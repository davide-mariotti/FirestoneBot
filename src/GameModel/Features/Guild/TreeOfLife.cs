using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild;

/// <summary>The Personal Tree only - the Guild Tree is shared and needs officer rank.</summary>
public static class TreeOfLife
{
    // Matches treeOfLifePersonalUpgrade (0)..(19) one to one, in the wiki table's order.
    private static readonly string[] PersonalUpgradeNames =
    {
        "Attribute Damage", "Attribute Health", "Attribute Armor",
        "Energy Heroes", "Mana Heroes", "Rage Heroes",
        "Miner", "Battle Cry", "All Attributes", "Firestone Finder", "Firestone Effect",
        "Raining Gold", "Hero Level Up Cost", "Guardian Power",
        "Fist Fight", "Precision", "Magic Spells",
        "Tank Specialization", "Damage Specialization", "Healer Specialization"
    };

    // Bought before any other upgrade, cheapest first among themselves - a group, not a ranking. The
    // F2P guide's Steam source gives the tree's centre as Battlecry, Miner, Firestone Finder, Raining Gold.
    private static readonly HashSet<string> PriorityUpgrades = new()
    {
        "Raining Gold", "Firestone Finder", "Firestone Effect", "Battle Cry", "Miner"
    };

    public static int PersonalUpgradeCount => PersonalUpgradeNames.Length;

    public static bool IsPriority(int index) => PriorityUpgrades.Contains(PersonalUpgradeNames[index]);

    public static IEnumerator OpenPersonalTab => new GameButton(Paths.TreeOfLifeLoc.PersonalTabBtn).Click();

    public static GameButton PersonalNode(int index) => new(NodePath(index));

    // Cost grows only with an upgrade's own level, so the lowest level is also the cheapest.
    public static int PersonalNodeLevel(int index) =>
        new GameText(NodePath(index) + Paths.TreeOfLifeLoc.NodeLevelTxt).GetParsedInt();

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

    private static string NodePath(int index) =>
        $"{Paths.TreeOfLifeLoc.PersonalNodeRoot}/treeOfLifePersonalUpgrade ({index})";
}
