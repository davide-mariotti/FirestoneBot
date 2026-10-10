using System;
using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Oracle;

public static class OracleStore
{
    public static IEnumerator Close => new GameButton(Paths.MenusLoc.OracleStoreLoc.CloseBtn).Click();

    public static IEnumerator ClaimGift => new GameButton(Paths.MenusLoc.OracleStoreLoc.OraclesGiftBtn).Click();

    // The Free button before the claim, the renew countdown after it.
    public static bool IsGiftShown =>
        new GameButton(Paths.MenusLoc.OracleStoreLoc.OraclesGiftBtn).IsVisible() || RenewTxt.IsVisible();

    public static bool IsClaimed => RenewTxt.IsVisible();

    public static int GiftCount => new GameText(Paths.MenusLoc.OracleStoreLoc.OraclesGiftCountTxt).GetParsedInt(-1);

    public static DateTime NextRunTime => RenewTxt.Time;

    private static GameText RenewTxt => new(Paths.MenusLoc.OracleStoreLoc.OraclesGiftRenewTxt);
}
