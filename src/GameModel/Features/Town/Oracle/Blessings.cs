using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Oracle;

public class Blessings : GameElement
{
    public Blessings() : base(Paths.MenusLoc.OracleLoc.BlessingLoc.Blessings) { }

    public int Upgraded { get; private set; }

    /// <summary>One level on each blessing whose dot is lit; the badge brings the task back for more.</summary>
    public IEnumerator UpgradeLit()
    {
        var lit = GetChildren()
            .Where(child => new GameElement(Paths.MenusLoc.OracleLoc.BlessingLoc.NotificationImg, child).IsVisible())
            .ToList();

        foreach (var blessing in lit)
        {
            yield return new GameButton(parent: blessing).Click();

            var buy = new GameButton(Paths.MenusLoc.OracleLoc.BlessingLoc.PreviewUpgradeBtn);
            yield return Poll.Until(buy.IsVisible);
            if (buy.IsClickable())
            {
                yield return buy.Click();
                Upgraded++;
            }

            if (CurrencyMissingPopup.IsShowing) yield return CurrencyMissingPopup.Close;
            yield return new GameButton(Paths.MenusLoc.OracleLoc.BlessingLoc.PreviewCloseBtn).Click();
        }
    }
}
