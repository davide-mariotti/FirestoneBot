using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Spends every Arcane Crystal on Awakening (the game picks the hero). The biggest usable
///     multiplier is re-selected before each awaken: cost is proportional, so it only saves clicks,
///     and re-selecting steps down as the crystals run low.
/// </summary>
public class AwakeningTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 50;

    // Everything on hand is spent, so a lit badge always means there's work.
    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Awakening;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private const int MaxIterations = 200;

    public override IEnumerator Execute()
    {
        yield return Notifications.Awakening;

        yield return TownGuild.Open;
        yield return TownGuild.OpenAwakening;

        for (var i = 0; i < MaxIterations; i++)
        {
            yield return Awakening.SelectBestMultiplier();

            if (!Awakening.AwakenBtn.IsClickable()) break;

            yield return Awakening.Awaken();
        }

        yield return Awakening.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
