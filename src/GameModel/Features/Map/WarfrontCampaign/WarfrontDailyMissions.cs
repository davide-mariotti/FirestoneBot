using System;
using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Map.WarfrontCampaign;

public static class WarfrontDailyMissions
{
    public static bool IsVisible => new GameElement(Paths.WFDailyMissionsLoc.CloseBtn).IsVisible();

    // Both buttons intermittently ignore a click, hence the retrying opens.
    public static IEnumerator Open => Poll.ClickUntil(
        () => new GameButton(Paths.WorldMapLoc.WarfrontLoc.DailyMissionsBtn), () => IsVisible, "WFDailyMissions");

    public static IEnumerator OpenLiberationMissions => Poll.ClickUntil(
        () => new GameButton(Paths.WFDailyMissionsLoc.OpenLiberationMissionsBtn),
        () => WarfrontLiberationMissions.IsVisible, "WFLiberationMissions");

    public static DateTime NextRunTime => new GameText(Paths.WFDailyMissionsLoc.NextRunTimeTxt).Time;

    public static IEnumerator Close => new GameButton(Paths.WFDailyMissionsLoc.CloseBtn).Click();
}

public static class WarfrontLiberationMissions
{
    public static bool IsVisible => new GameElement(Paths.WFLiberationMissionsLoc.CloseBtn).IsVisible();

    public static GameElement MissionsGrid => new(Paths.WFLiberationMissionsLoc.MissionsGridRoot);

    /// <summary>
    ///     The mission cells populate a moment after the screen opens. Stops early when the screen
    ///     isn't open at all, and waits the whole bound on days when every mission is already won.
    /// </summary>
    public static IEnumerator WaitUntilLoaded() => Poll.Until(
        () => !IsVisible || MissionsGrid.GetChildren().Any(HasClickableFightButton), 25, 0.3f);

    private static bool HasClickableFightButton(GameElement mission) =>
        new GameButton(Paths.WFLiberationMissionsLoc.FightBtn, mission).IsClickable();

    public static IEnumerator Close => new GameButton(Paths.WFLiberationMissionsLoc.CloseBtn).Click();
}

/// <summary>The formation preview a fight button may open; only its start button is pressed.</summary>
public static class WFBattleSim
{
    public static bool IsVisible => new GameElement(Paths.WFBattleSimLoc.FightBtn).IsVisible();

    public static IEnumerator Fight => new GameButton(Paths.WFBattleSimLoc.FightBtn).Click();
}

/// <summary>The live battle, which a fight button can also open directly. Only ever detected, never clicked.</summary>
public static class WFBattle
{
    public static bool IsVisible => new GameElement(Paths.WFBattleLoc.Root).IsVisible();
}

public static class WFBattleResult
{
    public static bool IsWon => new GameElement(Paths.WFBattleWonLoc.CloseBtn).IsVisible();

    public static bool IsDecided => IsWon || new GameElement(Paths.WFBattleDefeatLoc.CloseBtn).IsVisible();

    public static IEnumerator Close
    {
        get
        {
            yield return new GameButton(Paths.WFBattleWonLoc.CloseBtn).Click();
            yield return new GameButton(Paths.WFBattleDefeatLoc.CloseBtn).Click();
        }
    }
}
