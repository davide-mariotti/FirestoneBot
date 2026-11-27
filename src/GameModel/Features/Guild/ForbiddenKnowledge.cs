using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Il2Cpp;

namespace Firebot.GameModel.Features.Guild;

public static class ForbiddenKnowledge
{
    public static bool IsVisible => new GameElement(Paths.ForbiddenKnowledgeLoc.Root).IsVisible();

    public static IEnumerator OpenKramatakTab => new GameButton(Paths.ForbiddenKnowledgeLoc.KramatakTabBtn).Click();
    public static IEnumerator OpenLedraTab => new GameButton(Paths.ForbiddenKnowledgeLoc.LedraTabBtn).Click();
    public static IEnumerator OpenYamanothTab => new GameButton(Paths.ForbiddenKnowledgeLoc.YamanothTabBtn).Click();

    public static GameButton Node(int index) => new(Paths.ForbiddenKnowledgeLoc.NodeBtn(index));

    /// <summary>
    ///     A board from the game's data, no screen: the god's tomes, its nodes below the board's cap
    ///     (every node has to reach it before it rises by 5), what a level costs, and whether recruiting
    ///     the god is open. Null when it can't be read.
    /// </summary>
    public static (int Tomes, int Open, int Cost, bool Recruit)? Board(DarkGods god) => GameState.Get($"forbidden knowledge {god}", () =>
    {
        var h = GameState.H.ForbiddenKnowledgeHandler;
        var cap = god switch { DarkGods.Ledra => h.ledraSoftCap, DarkGods.Yamanoth => h.yamanothSoftCap, _ => h.kramatakSoftCap };
        var open = 0;
        foreach (var kv in h.darkGodDicts[god])
            if (kv.Value.Item2 < cap)
                open++;
        var tome = god switch
        {
            DarkGods.Ledra => CurrencyCode.TomeOfPowerLedra, DarkGods.Yamanoth => CurrencyCode.TomeOfPowerYamanoth, _ => CurrencyCode.TomeOfPowerKramatak
        };
        var tomes = (int)GameState.H.CurrencyHandler.GetCurrency(tome).Amount;
        return (tomes, open, ForbiddenKnowledgeHandler.upgradeCost, h.HasResearchToBuyGod(god) && tomes >= h.GodUnlockResearchCost);
    });

    /// <summary>The node is below the board's cap, from its own component; true when that can't be read.</summary>
    public static bool BelowCap(int index) =>
        GameState.Get("forbidden knowledge node", () =>
            GameElement.FindTransform(Paths.ForbiddenKnowledgeLoc.NodeBtn(index))?.GetComponent<ForbiddenKnowledgeUpgradeInteraction>() is { } n
                ? n.level < n.softCapLevel
                : true) ?? true;

    /// <summary>Buys in the preview a node click opens - a no-op for an already maxed node - then closes it.</summary>
    public static IEnumerator ConfirmUpgrade()
    {
        var upgradeBtn = new GameButton(Paths.ForbiddenKnowledgeLoc.PreviewUpgradeBtn);
        if (upgradeBtn.IsClickable()) yield return upgradeBtn.Click();

        yield return new GameButton(Paths.ForbiddenKnowledgeLoc.PreviewCloseBtn).Click();
    }

    /// <summary>Only clickable once the whole board is maxed and 50 of that god's tomes are on hand.</summary>
    public static IEnumerator TryRecruit()
    {
        var recruitBtn = new GameButton(Paths.ForbiddenKnowledgeLoc.RecruitBtn);
        if (recruitBtn.IsClickable()) yield return recruitBtn.Click();
    }

    public static IEnumerator Close => new GameButton(Paths.ForbiddenKnowledgeLoc.CloseBtn).Click();
}
