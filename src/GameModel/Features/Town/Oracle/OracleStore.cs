using System;
using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Oracle;

public static class OracleStore
{
    public static IEnumerator Close => new GameButton(Paths.MenusLoc.OracleStoreLoc.CloseBtn).Click();

    // At 10 fps the store shows up 2-3 s after the click that opens it (10/10).
    public static IEnumerator OpenGiftTab => OpenTab(Paths.MenusLoc.OracleStoreLoc.GiftTabBtn);

    public static IEnumerator OpenDailyTab => OpenTab(Paths.MenusLoc.OracleStoreLoc.DailyTabBtn);

    private static IEnumerator OpenTab(string path)
    {
        var tab = new GameButton(path);
        yield return Poll.Until(tab.IsVisible, 20);
        yield return tab.Click();
    }

    public static IEnumerator ClaimGift => new GameButton(Paths.MenusLoc.OracleStoreLoc.OraclesGiftBtn).Click();

    // The Free button before the claim, the renew countdown after it.
    public static bool IsGiftShown =>
        new GameButton(Paths.MenusLoc.OracleStoreLoc.OraclesGiftBtn).IsVisible() || RenewTxt.IsVisible();

    public static bool IsClaimed => RenewTxt.IsVisible();

    public static int GiftCount => new GameText(Paths.MenusLoc.OracleStoreLoc.OraclesGiftCountTxt).GetParsedInt(-1);

    public static DateTime NextRunTime => RenewTxt.Time;

    /// <summary>The tabs whose dot is lit, by their object name.</summary>
    public static string[] LitTabs => new GameElement(Paths.MenusLoc.OracleStoreLoc.TabButtons).GetChildren()
        .Where(tab => new GameElement(Paths.MenusLoc.OracleStoreLoc.TabNotificationImg, tab).IsVisible())
        .Select(tab => tab.Name)
        .ToArray();

    public static bool IsDailyGiftClaimable =>
        new GameButton(Paths.MenusLoc.OracleStoreLoc.DailyGiftBtn).IsClickable() &&
        !new GameElement(Paths.MenusLoc.OracleStoreLoc.DailyGiftRenewTxt).IsVisible();

    public static IEnumerator ClaimDailyGift => new GameButton(Paths.MenusLoc.OracleStoreLoc.DailyGiftBtn).Click();

    private static GameText RenewTxt => new(Paths.MenusLoc.OracleStoreLoc.OraclesGiftRenewTxt);
}
