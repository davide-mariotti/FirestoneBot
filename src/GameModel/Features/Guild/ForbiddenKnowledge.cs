using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild;

public static class ForbiddenKnowledge
{
    public static bool IsVisible => new GameElement(Paths.ForbiddenKnowledgeLoc.Root).IsVisible();

    public static IEnumerator OpenKramatakTab => new GameButton(Paths.ForbiddenKnowledgeLoc.KramatakTabBtn).Click();
    public static IEnumerator OpenLedraTab => new GameButton(Paths.ForbiddenKnowledgeLoc.LedraTabBtn).Click();
    public static IEnumerator OpenYamanothTab => new GameButton(Paths.ForbiddenKnowledgeLoc.YamanothTabBtn).Click();

    public static GameButton Node(int index) => new(Paths.ForbiddenKnowledgeLoc.NodeBtn(index));

    /// <summary>
    ///     Confirms the upgrade in the preview popup opened by clicking a node (live-confirmed by the
    ///     user, 2026-09-19: "Attribute damage / Level 0/5 / Upgrade [1 tome]"), same pattern as
    ///     TreeOfLife.ConfirmPurchase - safe no-op via IsClickable() if the node turned out to already
    ///     be maxed.
    /// </summary>
    public static IEnumerator ConfirmUpgrade()
    {
        var upgradeBtn = new GameButton(Paths.ForbiddenKnowledgeLoc.PreviewUpgradeBtn);
        if (upgradeBtn.IsClickable()) yield return upgradeBtn.Click();

        yield return new GameButton(Paths.ForbiddenKnowledgeLoc.PreviewCloseBtn).Click();
    }

    /// <summary>Safe no-op via IsClickable() unless every node on the current board is already maxed
    /// AND 50 of that god's tomes are on hand - see the wiki note in Paths.ForbiddenKnowledgeLoc.</summary>
    public static IEnumerator TryRecruit()
    {
        var recruitBtn = new GameButton(Paths.ForbiddenKnowledgeLoc.RecruitBtn);
        if (recruitBtn.IsClickable()) yield return recruitBtn.Click();
    }

    public static IEnumerator Close => new GameButton(Paths.ForbiddenKnowledgeLoc.CloseBtn).Click();
}
