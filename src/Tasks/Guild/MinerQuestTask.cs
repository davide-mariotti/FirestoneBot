using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Progresses the daily quest "Miner" (hit the Arcane Crystal 5 times) - level 50 per the wiki
///     quest table. Always hits exactly 5 times regardless of pickaxe cost per hit: FreePickaxesTask
///     (Task 5) already trickles pickaxes in continuously (per the wiki, a free claim every 96
///     minutes = 15/day), so there's no real risk of running out just from this quest's 5 hits - per
///     the user, confirmed acceptable to just always spend them.
///     Per the user (2026-09-23): once today's 5 hits are done, skip the whole routine (no Guild/
///     ArcaneCrystal screen open) until the date changes - previously this repeated the 5 hits every
///     6h regardless of whether today's quest was already satisfied, wasting pickaxes and clicks
///     across 16 bot instances.
///     Never automated before.
/// </summary>
public class MinerQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;
    protected override int MinimumCharacterLevel => 50;

    // Live-confirmed, 2026-09-20 (BotManager's notification rail diagnostic dump): the badge really
    // does toggle correctly, resolving the old "not independently verified" doubt - safe to also use
    // for scheduling now, not just the opportunistic click below.
    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ArcaneCrystal;

    private const int HitCount = 5;
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private MelonPreferences_Entry<string> _lastDoneDate;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_lastDoneDate != null) return;

        _lastDoneDate = category.CreateEntry(
            "last_done_date",
            "",
            "Last Done Date",
            "(auto-managed, don't edit) - the last date today's 5 hits were already done. Skips the " +
            "whole task until the date changes."
        );
    }

    public override IEnumerator Execute()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        if (_lastDoneDate?.Value == today)
        {
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        // Fast path - see NotificationBadgeName above, safe no-op if not up.
        yield return Notifications.ArcaneCrystal;

        // Guaranteed path regardless of the notification - same reasoning as every other task.
        yield return TownGuild.Open;
        yield return TownGuild.OpenArcaneCrystal;

        // User-requested optimization: if the game's hit-quantity multiplier can be set to
        // exactly 5, one click covers all 5 required hits instead of 5 separate ones. Falls back
        // to the proven one-by-one clicks if "5" isn't one of the available multiplier options
        // (not live-confirmed - see ArcaneCrystal.TrySetQuantityTo5).
        yield return ArcaneCrystal.TrySetQuantityTo5();

        if (ArcaneCrystal.IsQuantitySetTo5)
            yield return ArcaneCrystal.Hit;
        else
            for (var i = 0; i < HitCount; i++)
                yield return ArcaneCrystal.Hit;

        yield return ArcaneCrystal.Close;
        yield return TownGuild.Close;

        if (_lastDoneDate != null) _lastDoneDate.Value = today;
        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
