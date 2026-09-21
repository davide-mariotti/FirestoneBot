using System;
using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Oracle;

public static class OracleStore
{
    public static IEnumerator Close => new GameButton(Paths.MenusLoc.OracleStoreLoc.CloseBtn).Click();

    public static IEnumerator ClaimGift => new GameButton(Paths.MenusLoc.OracleStoreLoc.OraclesGiftBtn).Click();

    public static DateTime NextRunTime =>
        new GameText(Paths.MenusLoc.OracleStoreLoc.OraclesGiftRenewTxt).Time;
}
