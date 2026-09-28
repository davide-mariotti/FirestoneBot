using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild.Expeditions;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Guild;

/// <summary>Collects the finished guild expedition and starts the first pending one.</summary>
public class ExpeditionTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 10;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Expeditions;

    public override IEnumerator Execute()
    {
        yield return Notifications.Expeditions;

        yield return TownGuild.Open;
        yield return TownGuild.OpenExpeditions;

        yield return Expedition.Claim;
        yield return Expedition.Start;

        NextRunTime = Expedition.IsExpeditionActive ? Expedition.CurrentRunTime : Expedition.NextRunTime;

        yield return Expedition.Close;
        yield return TownGuild.Close;
    }
}
