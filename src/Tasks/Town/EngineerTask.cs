using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Engineer = Firebot.GameModel.Features.Town.Engineer.Engineer;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>Claims the Engineer's tools, ready every six hours.</summary>
public class EngineerTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 50;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Engineer;

    public override IEnumerator Execute()
    {
        yield return Notifications.Engineer;

        yield return TownScreen.Open;
        yield return TownScreen.OpenEngineer;

        yield return Engineer.Claim;
        NextRunTime = Engineer.NextRunTime;

        yield return Engineer.Close;
        yield return TownScreen.Close;
    }
}
