using System;
using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Map.WarfrontCampaign;

public static class WarfrontDailyMissions
{
    public static bool IsVisible => new GameElement(Paths.WFDailyMissionsLoc.CloseBtn).IsVisible();

    private static readonly WaitForSeconds OpenPollWait = new(0.5f);
    private const int MaxOpenPolls = 10; // ~5s
    private const int MaxClickAttempts = 3;

    /// <summary>
    ///     Live-confirmed, 2026-09-24 (user report on Steam-10): plain Click() on dailyMissionsButton
    ///     was intermittently not opening the WFDailyMissions hub at all, leaving the task stuck
    ///     re-entering/re-exiting the World Map every retry cycle without ever reaching the mission
    ///     list - same "Button technically clickable but onClick has no real effect" shape already
    ///     fixed for Store.Open/EventManager.Open/the liberation fightBtn. ClickSimulated + poll +
    ///     retry, same pattern as those fixes.
    /// </summary>
    public static IEnumerator Open => ClickAndConfirm(
        () => new GameButton(Paths.WorldMapLoc.WarfrontLoc.DailyMissionsBtn), () => IsVisible, "WFDailyMissions hub");

    /// <summary>Same click-reliability issue as Open above, confirmed on the same live report.</summary>
    public static IEnumerator OpenLiberationMissions => ClickAndConfirm(
        () => new GameButton(Paths.WFDailyMissionsLoc.OpenLiberationMissionsBtn),
        () => WarfrontLiberationMissions.IsVisible, "WFLiberationMissions list");

    private static IEnumerator ClickAndConfirm(Func<GameButton> button, Func<bool> opened, string label)
    {
        for (var attempt = 1; attempt <= MaxClickAttempts && !opened(); attempt++)
        {
            yield return button().ClickSimulated();

            var pollsLeft = MaxOpenPolls;
            while (!opened() && pollsLeft > 0)
            {
                yield return OpenPollWait;
                pollsLeft--;
            }

            Logger.Debug($"[WarfrontDailyMissions] Open '{label}' attempt {attempt}/{MaxClickAttempts}: visible={opened()}.");
        }
    }

    public static DateTime NextRunTime => new GameText(Paths.WFDailyMissionsLoc.NextRunTimeTxt).Time;

    public static IEnumerator Close => new GameButton(Paths.WFDailyMissionsLoc.CloseBtn).Click();
}

public static class WarfrontLiberationMissions
{
    public static bool IsVisible => new GameElement(Paths.WFLiberationMissionsLoc.CloseBtn).IsVisible();

    // Live-confirmed, 2026-09-18: right after opening, every pooled "liberationMission (N)" cell's
    // fightButton read as hidden/inactive - the ScrollView's cells need a moment to populate, same
    // pattern as Inventory's chest list (see CollectorQuestTask.ChestListPopulateDelay), but polled
    // instead of a fixed wait since the exact delay isn't known.
    private static readonly WaitForSeconds PopulatePollWait = new(0.3f);
    private const int MaxPopulatePolls = 25; // ~7.5s ceiling

    public static GameElement MissionsGrid => new(Paths.WFLiberationMissionsLoc.MissionsGridRoot);

    // Live-confirmed, 2026-09-24: only waiting for a clickable mission span the full ~7.5s ceiling
    // every time on a day where all 10 are already won (nothing wrong, just wasted time) - also bail
    // immediately if the screen isn't even open (Open/OpenLiberationMissions above already retry the
    // click itself, so if we get here and it's still not visible, more polling here won't help).
    public static IEnumerator WaitUntilLoaded()
    {
        var pollsLeft = MaxPopulatePolls;
        while (pollsLeft > 0 && IsVisible && !MissionsGrid.GetChildren().Any(HasClickableFightButton))
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

/// <summary>
///     The real-time battle itself - see Paths.WFBattleLoc's doc comment: a liberation mission's
///     fightButton can open this DIRECTLY, skipping WFBattleSim's own formation-preview screen
///     entirely (live-confirmed, 2026-09-24, Steam-10). Nothing to click here, just something to
///     recognize so the bot knows the fight is already running instead of concluding it never started.
/// </summary>
public static class WFBattle
{
    public static bool IsVisible => new GameElement(Paths.WFBattleLoc.Root).IsVisible();
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
