using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Engineer = Firebot.GameModel.Features.Town.Engineer.Engineer;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

public class EngineerTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 50;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Engineer;

    public override IEnumerator Execute()
    {
        // Fast path: the notification (when up) opens Engineer directly. Safe no-op otherwise.
        yield return Notifications.Engineer;

        // Guaranteed path regardless of the notification - same reasoning as FreePickaxes/GuildShop:
        // don't rely on the screen already being open. Every click below is a safe no-op if that
        // step already happened via the notification.
        yield return TownScreen.Open;
        yield return TownScreen.OpenEngineer;

        yield return Engineer.Claim;
        NextRunTime = Engineer.NextRunTime;

        yield return Engineer.Close;
        yield return TownScreen.Close;
    }
}
