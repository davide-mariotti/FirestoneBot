using System;
using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

namespace Firebot.GameModel.Features.Map.WarfrontCampaign;

public static class WarfrontDailyMissions
{
    public static IEnumerator Open => new GameButton(Paths.WorldMapLoc.WarfrontLoc.DailyMissionsBtn).Click();

    public static IEnumerator OpenLiberationMissions =>
        new GameButton(Paths.WFDailyMissionsLoc.OpenLiberationMissionsBtn).Click();

    public static DateTime NextRunTime => new GameText(Paths.WFDailyMissionsLoc.NextRunTimeTxt).Time;

    public static IEnumerator Close => new GameButton(Paths.WFDailyMissionsLoc.CloseBtn).Click();
}

public static class WarfrontLiberationMissions
{
    // Live-confirmed, 2026-09-18: right after opening, every pooled "liberationMission (N)" cell's
    // fightButton read as hidden/inactive - the ScrollView's cells need a moment to populate, same
    // pattern as Inventory's chest list (see CollectorQuestTask.ChestListPopulateDelay), but polled
    // instead of a fixed wait since the exact delay isn't known.
    private static readonly WaitForSeconds PopulatePollWait = new(0.3f);
    private const int MaxPopulatePolls = 25; // ~7.5s ceiling

    public static GameElement MissionsGrid => new(Paths.WFLiberationMissionsLoc.MissionsGridRoot);

    public static IEnumerator WaitUntilLoaded()
    {
        var pollsLeft = MaxPopulatePolls;
        while (pollsLeft > 0 && !MissionsGrid.GetChildren().Any(HasClickableFightButton))
        {
            yield return PopulatePollWait;
            pollsLeft--;
        }
    }

    private static bool HasClickableFightButton(GameElement mission) =>
        new GameButton(Paths.WFLiberationMissionsLoc.FightBtn, mission).IsClickable();

    public static IEnumerator Close => new GameButton(Paths.WFLiberationMissionsLoc.CloseBtn).Click();
}

/// <summary>Squad/formation preview opened by a liberation mission's fightButton - the user sets the
/// formation once manually, so this only ever needs to press the "start" button.</summary>
public static class WFBattleSim
{
    public static bool IsVisible => new GameElement(Paths.WFBattleSimLoc.FightBtn).IsVisible();

    public static IEnumerator Fight => new GameButton(Paths.WFBattleSimLoc.FightBtn).Click();
}

/// <summary>The Won/Defeat popup a liberation battle resolves into - see WFBattleSim.Fight.</summary>
public static class WFBattleResult
{
    public static bool IsDecided =>
        new GameElement(Paths.WFBattleWonLoc.CloseBtn).IsVisible() ||
        new GameElement(Paths.WFBattleDefeatLoc.CloseBtn).IsVisible();

    public static IEnumerator Close
    {
        get
        {
            yield return new GameButton(Paths.WFBattleWonLoc.CloseBtn).Click();
            yield return new GameButton(Paths.WFBattleDefeatLoc.CloseBtn).Click();
        }
    }
}
