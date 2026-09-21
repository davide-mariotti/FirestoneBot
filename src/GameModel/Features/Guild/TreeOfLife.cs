using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild;

/// <summary>Personal Tree only (Guild Tree is never touched - shared with guildmates, requires
/// leader/officer rank per the wiki, out of scope per the user).</summary>
public static class TreeOfLife
{
    // Names in tier order, matching treeOfLifePersonalUpgrade (0)-(19) 1:1 - confirmed via UnityPy +
    // the wiki's Personal Tree table (exactly 20 rows, same count as the live node scan).
    private static readonly string[] PersonalUpgradeNames =
    {
        "Attribute Damage", "Attribute Health", "Attribute Armor",
        "Energy Heroes", "Mana Heroes", "Rage Heroes",
        "Miner", "Battle Cry", "All Attributes", "Firestone Finder", "Firestone Effect",
        "Raining Gold", "Hero Level Up Cost", "Guardian Power",
        "Fist Fight", "Precision", "Magic Spells",
        "Tank Specialization", "Damage Specialization", "Healer Specialization"
    };

    // Per the user: prioritize this group of 3 over the other 17 - not strictly ranked against each
    // other, the cheapest (lowest current level) of whichever are affordable wins, same as the other
    // two within their own tier. Mirrors the "Raining Gold always wins" override already used for
    // Firestone/Meteorite Research, generalized from one name to a small set.
    private static readonly HashSet<string> PriorityUpgrades = new()
    {
        "Raining Gold", "Firestone Finder", "Firestone Effect"
    };

    public static int PersonalUpgradeCount => PersonalUpgradeNames.Length;

    public static bool IsPriority(int index) => PriorityUpgrades.Contains(PersonalUpgradeNames[index]);

    public static IEnumerator OpenPersonalTab => new GameButton(Paths.TreeOfLifeLoc.PersonalTabBtn).Click();

    public static GameButton PersonalNode(int index) => new(NodePath(index));

    /// <summary>Current level, read directly off the grid node - no separate preview popup was found
    /// for this feature (unlike Talents/Firestone Research/Meteorite Research), so this doubles as
    /// the cost tie-break: the wiki confirms cost scales purely with an upgrade's own current level
    /// (uniformly across all 20), so "lowest level" and "cheapest to buy next" are the same thing.</summary>
    public static int PersonalNodeLevel(int index) =>
        new GameText(NodePath(index) + Paths.TreeOfLifeLoc.NodeLevelTxt).GetParsedInt();

    public static IEnumerator Close => new GameButton(Paths.TreeOfLifeLoc.CloseBtn).Click();

    /// <summary>
    ///     Confirms the purchase in the preview popup opened by clicking a node (live-confirmed,
    ///     2026-09-18 - PersonalNode's click alone only opens this popup, it doesn't buy directly).
    ///     Safe no-op via IsClickable() if the upgrade turned out to be maxed (no buy button in that
    ///     state). Always attempts the close after, in case buying doesn't auto-dismiss the popup.
    ///     Live-confirmed, 2026-09-18: buyUpgradeButton's own IsClickable() doesn't reliably predict
    ///     real affordability - clicking it while short on Expedition Tokens pops the game's generic
    ///     "You need N more..." validation message (see HasInsufficientFundsMessage) instead of
    ///     silently failing. Check that after calling this and stop the caller's loop if it's up.
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
