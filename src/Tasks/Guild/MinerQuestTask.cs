using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;

namespace Firebot.Tasks.Guild;

/// <summary>
///     The daily "Miner" quest: 5 hits on the Guild's Arcane Crystal. FreePickaxesTask keeps the
///     pickaxes coming, so the hits are always spent. Once done, the task waits for the next game-day.
/// </summary>
public class MinerQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;
    protected override int MinimumCharacterLevel => 50;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ArcaneCrystal;

    // The Arcane Crystal badge stays lit after today's hits are done - it tracks something else on
    // that screen - so it only counts while the quest is still open.
    public override bool IsNotificationVisible() => base.IsNotificationVisible() && _lastDoneDate?.Value != GameDay.Today();

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
        var today = GameDay.Today();
        if (_lastDoneDate?.Value == today)
        {
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return Notifications.ArcaneCrystal;

        yield return TownGuild.Open;
        yield return TownGuild.OpenArcaneCrystal;

        // One x5 click if that multiplier exists, else 5 single hits.
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
