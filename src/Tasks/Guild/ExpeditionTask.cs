using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild.Expeditions;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Guild;

public class ExpeditionTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 10;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Expeditions;

    public override IEnumerator Execute()
    {
        // Fast path: the notification (when up) opens Expeditions directly. Safe no-op otherwise.
        yield return Notifications.Expeditions;

        // Guaranteed path regardless of the notification - same reasoning as Free Pickaxes/Engineer:
        // don't rely on the popup already being open. Every click below is a safe no-op if that step
        // already happened via the notification.
        yield return TownGuild.Open;
        yield return TownGuild.OpenExpeditions;

        yield return Expedition.Claim;
        yield return Expedition.Start;

        NextRunTime = Expedition.IsExpeditionActive ? Expedition.CurrentRunTime : Expedition.NextRunTime;

        yield return Expedition.Close;
        yield return TownGuild.Close;
    }
}
