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
