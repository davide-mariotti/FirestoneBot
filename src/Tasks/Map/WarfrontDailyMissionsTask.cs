using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Map;
using Firebot.GameModel.Features.Map.WarfrontCampaign;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;
using Logger = Firebot.Core.Logger;

namespace Firebot.Tasks.Map;

/// <summary>
///     Fights the Warfront's daily liberation missions (not the dungeons), one real battle at a time,
///     which also completes the daily "Liberator" quest. The formation is the active squad, which
///     WarfrontCampaignTask keeps at its strongest; this only presses start.
/// </summary>
public class WarfrontDailyMissionsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Warfront;
    protected override int MinimumCharacterLevel => 50;

    protected override string DisplayName => "Daily Missions";

    // Real liberation battles take well under a minute.
    private const int MaxBattlePolls = 40;
    private const float BattlePollSeconds = 1f;

    // A fight button sometimes ignores the click entirely, so the click itself is retried.
    private const int MaxSimOpenPolls = 10;
    private const float SimOpenPollSeconds = 0.5f;
    private const int MaxFightAttempts = 3;

    // A click the game took leaves every fight button disabled until the answer, which can be late:
    // the server's refusal comes ~10 s on, and the Achievements menu can open ahead of the battle.
    private const int MaxAnswerPolls = 40;

    private static bool FightAnswered =>
        WFBattleSim.IsVisible || WFBattle.IsVisible || WFBattleResult.IsDecided || WarfrontLiberationMissions.FightRefused;

    private MelonPreferences_Entry<string> _restartedOn;

    private static readonly TimeSpan RetryAfterReset = TimeSpan.FromMinutes(5);

    // Asks the game, not the clock: missions also turn up away from the bot's reset, with the stars
    // for a new one or when the game moves its reset (06/11, all 32 accounts sat on them for a day).
    // The badge on the daily-missions button shows only with the map open, and the rail's is the loot task's.
    public override bool IsNotificationVisible() =>
        IsEnabled && MeetsLevelRequirement && WarfrontLiberationMissions.HasAvailable;

    protected override void OnConfigure(MelonPreferences_Category category) =>
        _restartedOn ??= category.CreateEntry("restarted_on", "", "Restarted On",
            "(auto-managed, don't edit) - the last game-day a refused fight restarted the game.");

    // 07/11, Steam-3, -4, -17, -25, -26 and -30: a fight that reached an achievement opened
    // menus/Achievements instead of the battle. The server counted the mission, the client still offers
    // it, and each click on it gets "An error occurred on the server", which leaves the other missions'
    // buttons dead too. Only reloading the client clears it. Once a game-day, so an outage can't loop.
    private void RestartOnceToday()
    {
        if (_restartedOn.Value == GameDay.Today()) return;

        _restartedOn.Value = GameDay.Today();
        PersistNextRunTime();
        Logger.Info("[WarfrontDailyMissionsTask] The server refused a mission the game still offers - closing the game.");
        Process.GetCurrentProcess().Kill();
    }

    public override IEnumerator Execute()
    {
        yield return WorldMap.Open;
        yield return WorldMap.OpenWarfrontCampaignTab;
        yield return WarfrontDailyMissions.Open;
        yield return WarfrontDailyMissions.OpenLiberationMissions;
        yield return WarfrontLiberationMissions.WaitUntilLoaded();

        var missions = WarfrontLiberationMissions.MissionsGrid.GetChildren().ToList();
        Logger.Debug($"[WarfrontDailyMissionsTask] {missions.Count} mission cell(s) found in grid.");

        var fought = 0;
        for (var i = 0; i < missions.Count; i++)
        {
            var mission = missions[i];
            var fightBtn = new GameButton(Paths.WFLiberationMissionsLoc.FightBtn, mission);
            var clickable = fightBtn.IsClickable();
            Logger.Debug($"[WarfrontDailyMissionsTask] Mission {i}: fightBtn.IsClickable={clickable}, " +
                         $"screenOpen={WarfrontLiberationMissions.IsVisible}.");

            if (!clickable) continue;

            // The fight button opens either the formation preview (WFBattleSim) or, when the last
            // formation is still accepted, the battle itself - both mean the fight has started.
            var simOpened = false;
            var battleAlreadyRunning = false;
            for (var attempt = 1; attempt <= MaxFightAttempts && !simOpened && !battleAlreadyRunning; attempt++)
            {
                if (attempt > 1 && !fightBtn.IsClickable())
                {
                    Logger.Debug($"[WarfrontDailyMissionsTask] Mission {i} attempt {attempt}: fightBtn no longer " +
                                 "clickable - the list likely closed, no point retrying this exact click again.");
                    break;
                }

                yield return fightBtn.Click();
                yield return Poll.Until(() => FightAnswered, MaxSimOpenPolls, SimOpenPollSeconds);
                if (!FightAnswered && !fightBtn.IsClickable())
                    yield return Poll.Until(() => FightAnswered, MaxAnswerPolls, SimOpenPollSeconds);

                simOpened = WFBattleSim.IsVisible;
                battleAlreadyRunning = WFBattle.IsVisible || WFBattleResult.IsDecided;
                Logger.Debug($"[WarfrontDailyMissionsTask] Mission {i} attempt {attempt}/{MaxFightAttempts}: " +
                             $"WFBattleSim visible={simOpened}, battle already running={battleAlreadyRunning}, " +
                             $"screenOpen={WarfrontLiberationMissions.IsVisible}.");
            }

            if (WarfrontLiberationMissions.FightRefused) RestartOnceToday();

            if (!simOpened && !battleAlreadyRunning)
            {
                // With the list gone too, the remaining entries are stale - stop instead of walking them.
                if (!WarfrontLiberationMissions.IsVisible)
                {
                    Logger.Debug($"[WarfrontDailyMissionsTask] Mission list closed after {MaxFightAttempts} attempt(s) " +
                                 $"on mission {i} - stopping this pass. Open popups/menus: {Watchdog.DumpActiveScreens()}");
                    break;
                }

                continue; // e.g. the mission turned out locked or already won
            }

            if (simOpened) yield return WFBattleSim.Fight;

            yield return Poll.Until(() => WFBattleResult.IsDecided, MaxBattlePolls, BattlePollSeconds);

            if (!WFBattleResult.IsDecided)
                Logger.Warning("[WarfrontDailyMissionsTask] Liberation battle didn't resolve within the wait bound.");
            else
                fought++;

            yield return WFBattleResult.Close;

            Logger.Debug($"[WarfrontDailyMissionsTask] Mission {i}: after Close - " +
                         $"WFBattleSim.IsVisible={WFBattleSim.IsVisible}, screenOpen={WarfrontLiberationMissions.IsVisible}.");
        }

        Logger.Debug($"[WarfrontDailyMissionsTask] Done: fought {fought}/{missions.Count} mission(s).");

        yield return WarfrontLiberationMissions.Close;

        // Right after the reset the list can still show yesterday's: on 03/10 every run that ended by
        // 10:00:17 fought 0 of 10 (Steam-1, -6, -8, -15) and every one from 10:00:27 fought 2. Nothing
        // fought in the reset's first hour means try again shortly, not tomorrow.
        // The missions renew with the day, whatever the popup's countdown says: on 04/11 it read 37 h on
        // Steam-0, which then skipped the 05/11 reset.
        var nextReset = WarfrontDailyMissions.NextRunTime;
        if (nextReset > GameDay.NextReset()) nextReset = GameDay.NextReset();
        NextRunTime = fought == 0 && nextReset - DateTime.Now > TimeSpan.FromHours(23) ? DateTime.Now + RetryAfterReset : nextReset;

        yield return WarfrontDailyMissions.Close;
        yield return WorldMap.Close;
    }
}
