using System.Collections;
using System.Linq;
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

        foreach (var mission in WarfrontLiberationMissions.MissionsGrid.GetChildren())
        {
            var fightBtn = new GameButton(Paths.WFLiberationMissionsLoc.FightBtn, mission);
            if (!fightBtn.IsClickable()) continue;

            yield return fightBtn.Click(); // opens WFBattleSim (squad already set up by the user)
            if (!WFBattleSim.IsVisible) continue; // e.g. mission turned out locked/already resolved

            yield return WFBattleSim.Fight; // starts the real battle

            var pollsLeft = MaxBattlePolls;
            while (!WFBattleResult.IsDecided && pollsLeft > 0)
            {
                yield return BattlePollWait;
                pollsLeft--;
            }

            if (pollsLeft == 0)
                Logger.Warning("[WarfrontDailyMissionsTask] Liberation battle didn't resolve within the wait bound.");

            yield return WFBattleResult.Close;
        }

        yield return WarfrontLiberationMissions.Close;

        NextRunTime = WarfrontDailyMissions.NextRunTime;

        yield return WarfrontDailyMissions.Close;
        yield return WorldMap.Close;
    }
}
