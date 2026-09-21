using System;
using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild.Expeditions;

public static class Expedition
{
    public static IEnumerator Close => new GameButton(Paths.MenusLoc.ExpeditionsLoc.CloseBtn).Click();

    public static bool IsExpeditionActive =>
        new GameElement(Paths.MenusLoc.ExpeditionsLoc.ActiveExpedition).IsVisible();

    public static DateTime NextRunTime => new GameText(Paths.MenusLoc.ExpeditionsLoc.NextRunTimeTxt).Time;

    public static IEnumerator Claim => new GameButton(Paths.MenusLoc.ExpeditionsLoc.ClaimBtn).Click();

    public static DateTime CurrentRunTime => new GameText(Paths.MenusLoc.ExpeditionsLoc.CurrentRunTimeTxt).Time;

    public static IEnumerator Start => new GameButton(Paths.MenusLoc.ExpeditionsLoc.StartBtn).Click();
}
