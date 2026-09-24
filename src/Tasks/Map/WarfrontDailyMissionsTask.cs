using System.Collections;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Map;
using Firebot.GameModel.Features.Map.WarfrontCampaign;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.Tasks.Map;

/// <summary>
///     The Warfront Campaign tab's "Daily Missions" hub - separate from WarfrontCampaignLootTask
///     (different badge, different screen path within the same tab). Only wires the "Liberation
///     Missions" category (fight for a listed reward, no currency involved); the hub also has a
///     "Dungeon Missions" category, out of scope for now. Also drives the "Liberator" daily quest
///     (fight 2 Warfront battles) - each fight is a real battle to wait out, not an instant
///     resolution (formation is set up manually by the user beforehand; the bot only presses
///     "start" on WFBattleSim).
/// </summary>
public class WarfrontDailyMissionsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Warfront;
    protected override int MinimumCharacterLevel => 50;

    // Cosmetic only, per the user (2026-09-20) - avoids the class-name-derived "Warfront Daily
    // Missions" repeating the group name in the terminal table ("Warfront - Warfront Daily Missions").
    protected override string DisplayName => "Daily Missions";

    private static readonly WaitForSeconds BattlePollWait = new(1f);

    // Live-confirmed, 2026-09-18: real liberation battles resolve in well under a minute (a handful
    // of rounds) - the original 5-minute bound was picked before ever observing a real one and made
    // getting stuck (see WFBattleResult.IsDecided) far more costly than it needed to be. Per the
    // user, 40s is a comfortable margin.
    private const int MaxBattlePolls = 40; // ~40s at 1s/poll

    // Live-confirmed, 2026-09-24: fightBtn.Click() was checked against WFBattleSim.IsVisible with NO
    // wait beyond Click()'s own ~1s interaction_delay - a live diagnostic caught a run where mission 1
    // reported WFBattleSim not open right after the click, yet the LiberationMissions list was ALSO
    // reported closed for every remaining mission from that point on. Polling instead of a single
    // check gives the transition the same margin every other screen-open in this codebase gets - but
    // polling alone (up to 5s) still didn't always help: a follow-up run needed a SECOND full task
    // execution (not just a longer wait within the same click) before the mission actually completed.
    // Same intermittent "click sometimes has no effect at all" shape as EventManager.Open's eventsButton
    // (see its own doc comment, fixed there with a click retry) - retries the click itself here too
    // (MaxFightAttempts) instead of relying on the next scheduled run to accidentally land it.
    private static readonly WaitForSeconds SimOpenPollWait = new(0.5f);
    private const int MaxSimOpenPolls = 10; // ~5s
    private const int MaxFightAttempts = 3;

    // Badge lives on the button itself (WorldMap/warfrontCampaignSubmenu/dailyMissionsButton), not
    // the battle-screen leftSideUINew rail - same situation as Path of Glory, so there's no separate
    // opportunistic click to try first; this click IS the fast path once we're on the right tab.
    protected override string NotificationPath => Paths.WorldMapLoc.WarfrontLoc.DailyMissionsNotification;

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

            // Live-confirmed, 2026-09-24 (Steam-10, via DumpOpenPopups): fightBtn can open WFBattleSim
            // (the formation preview, needing its own "start" press) OR skip straight to the real
            // battle (menus/WFBattle) - seemingly whenever the formation from a previous fight is
            // already accepted/unchanged, no preview needed. Code that only recognized WFBattleSim
            // concluded "didn't start" and abandoned the mission while the battle was already running
            // underneath, explaining "fought 0" runs where a real fight was actually in progress.
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

                var simPolls = MaxSimOpenPolls;
                while (!WFBattleSim.IsVisible && !WFBattle.IsVisible && !WFBattleResult.IsDecided && simPolls > 0)
                {
                    yield return SimOpenPollWait;
                    simPolls--;
                }

                simOpened = WFBattleSim.IsVisible;
                battleAlreadyRunning = WFBattle.IsVisible || WFBattleResult.IsDecided;
                Logger.Debug($"[WarfrontDailyMissionsTask] Mission {i} attempt {attempt}/{MaxFightAttempts}: " +
                             $"WFBattleSim visible={simOpened}, battle already running={battleAlreadyRunning} " +
                             $"(waited {MaxSimOpenPolls - simPolls} extra poll(s)), screenOpen={WarfrontLiberationMissions.IsVisible}.");
            }

            if (!simOpened && !battleAlreadyRunning)
            {
                // If the list itself is gone too, we're not on either screen the rest of this loop
                // expects - stop instead of burning through the remaining stale entries logging the
                // same "unclickable/closed" result for each one.
                if (!WarfrontLiberationMissions.IsVisible)
                {
                    Logger.Debug($"[WarfrontDailyMissionsTask] Mission list closed after {MaxFightAttempts} attempt(s) " +
                                 $"on mission {i} - stopping this pass. Open popups/menus: {Watchdog.DumpActiveScreens()}");
                    break;
                }

                continue; // e.g. mission turned out locked/already resolved
            }

            if (simOpened) yield return WFBattleSim.Fight; // starts the real battle - already running otherwise

            var pollsLeft = MaxBattlePolls;
            while (!WFBattleResult.IsDecided && pollsLeft > 0)
            {
                yield return BattlePollWait;
                pollsLeft--;
            }

            if (pollsLeft == 0)
                Logger.Warning("[WarfrontDailyMissionsTask] Liberation battle didn't resolve within the wait bound.");
            else
                fought++;

            yield return WFBattleResult.Close;

            Logger.Debug($"[WarfrontDailyMissionsTask] Mission {i}: after Close - " +
                         $"WFBattleSim.IsVisible={WFBattleSim.IsVisible}, screenOpen={WarfrontLiberationMissions.IsVisible}.");
        }

        Logger.Debug($"[WarfrontDailyMissionsTask] Done: fought {fought}/{missions.Count} mission(s).");

        yield return WarfrontLiberationMissions.Close;

        NextRunTime = WarfrontDailyMissions.NextRunTime;

        yield return WarfrontDailyMissions.Close;
        yield return WorldMap.Close;
    }
}
