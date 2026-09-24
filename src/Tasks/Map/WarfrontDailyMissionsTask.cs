using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Base;
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
                         $"screenOpen={IsLiberationMissionsScreenOpen()}.");

            if (!clickable) continue;

            var simOpened = false;
            for (var attempt = 1; attempt <= MaxFightAttempts && !simOpened; attempt++)
            {
                if (attempt > 1 && !fightBtn.IsClickable())
                {
                    Logger.Debug($"[WarfrontDailyMissionsTask] Mission {i} attempt {attempt}: fightBtn no longer " +
                                 "clickable - the list likely closed, no point retrying this exact click again.");
                    break;
                }

                yield return fightBtn.Click(); // opens WFBattleSim (squad already set up by the user)

                var simPolls = MaxSimOpenPolls;
                while (!WFBattleSim.IsVisible && simPolls > 0)
                {
                    yield return SimOpenPollWait;
                    simPolls--;
                }

                simOpened = WFBattleSim.IsVisible;
                Logger.Debug($"[WarfrontDailyMissionsTask] Mission {i} attempt {attempt}/{MaxFightAttempts}: " +
                             $"WFBattleSim visible={simOpened} (waited {MaxSimOpenPolls - simPolls} extra poll(s)), " +
                             $"screenOpen={IsLiberationMissionsScreenOpen()}.");
            }

            if (!simOpened)
            {
                // If the list itself is gone too, we're not on either screen the rest of this loop
                // expects - stop instead of burning through the remaining stale entries logging the
                // same "unclickable/closed" result for each one.
                if (!IsLiberationMissionsScreenOpen())
                {
                    Logger.Debug($"[WarfrontDailyMissionsTask] Mission list closed after {MaxFightAttempts} attempt(s) " +
                                 $"on mission {i} - stopping this pass. Open popups/menus: {DumpOpenPopups()}");
                    break;
                }

                continue; // e.g. mission turned out locked/already resolved
            }

            yield return WFBattleSim.Fight; // starts the real battle

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
                         $"WFBattleSim.IsVisible={WFBattleSim.IsVisible}, screenOpen={IsLiberationMissionsScreenOpen()}.");
        }

        Logger.Debug($"[WarfrontDailyMissionsTask] Done: fought {fought}/{missions.Count} mission(s).");

        yield return WarfrontLiberationMissions.Close;

        NextRunTime = WarfrontDailyMissions.NextRunTime;

        yield return WarfrontDailyMissions.Close;
        yield return WorldMap.Close;
    }

    // Diagnostic only, 2026-09-24: per the user, only the first liberation mission in a run ever
    // completes - the rest silently do nothing. Checks whether the WFLiberationMissions popup itself
    // is still open between missions, to see if it's getting left behind/deactivated by something
    // WFBattleSim or the battle result popups do on close, rather than a per-mission cell issue.
    private static bool IsLiberationMissionsScreenOpen() =>
        new GameElement(Paths.WFLiberationMissionsLoc.CloseBtn).IsVisible();

    // Diagnostic only, 2026-09-24: WFBattleSim never appeared even after a full 5s poll, yet
    // WFLiberationMissions also read as closed - something else is showing. Reuses Watchdog's own
    // known roots (read-only here, no clicking) to see which real popup/menu is actually active.
    private static string DumpOpenPopups()
    {
        var popups = new GameElement(Paths.WatchdogLoc.PopupsRoot).GetChildren()
            .Where(p => p.IsVisible()).Select(p => $"popups/{p.Name}");
        var menus = new GameElement(Paths.WatchdogLoc.MenusRoot).GetChildren()
            .Where(m => m.IsVisible()).Select(m => $"menus/{m.Name}");
        var events = new GameElement(Paths.WatchdogLoc.EventsRoot).GetChildren()
            .Where(e => e.IsVisible()).Select(e => $"events/{e.Name}");

        var all = popups.Concat(menus).Concat(events).ToList();
        return all.Count == 0 ? "(none active)" : string.Join(", ", all);
    }
}
