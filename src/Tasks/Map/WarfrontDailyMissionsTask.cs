using System;
using System.Collections;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Map;
using Firebot.GameModel.Features.Map.WarfrontCampaign;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
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

    private static readonly TimeSpan RetryAfterReset = TimeSpan.FromMinutes(5);

    // The badge sits on the daily-missions button itself, not on the notification rail.
    protected override string[] NotificationPaths => new[] { Paths.WorldMapLoc.WarfrontLoc.DailyMissionsNotification };

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
                yield return Poll.Until(() => WFBattleSim.IsVisible || WFBattle.IsVisible || WFBattleResult.IsDecided,
                    MaxSimOpenPolls, SimOpenPollSeconds);

                simOpened = WFBattleSim.IsVisible;
                battleAlreadyRunning = WFBattle.IsVisible || WFBattleResult.IsDecided;
                Logger.Debug($"[WarfrontDailyMissionsTask] Mission {i} attempt {attempt}/{MaxFightAttempts}: " +
                             $"WFBattleSim visible={simOpened}, battle already running={battleAlreadyRunning}, " +
                             $"screenOpen={WarfrontLiberationMissions.IsVisible}.");
            }

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
        var nextReset = WarfrontDailyMissions.NextRunTime;
        NextRunTime = fought == 0 && nextReset - DateTime.Now > TimeSpan.FromHours(23) ? DateTime.Now + RetryAfterReset : nextReset;

        yield return WarfrontDailyMissions.Close;
        yield return WorldMap.Close;
    }
}
