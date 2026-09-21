using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Oracle;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

public class OracleRitualsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 200;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.OracleRituals;

    public override IEnumerator Execute()
    {
        // Fast path: the notification (when up) opens Oracle directly. Safe no-op otherwise.
        yield return Notifications.OracleRituals;

        // Guaranteed path regardless of the notification - same reasoning as the previous tasks:
        // don't rely on the screen already being open.
        yield return TownScreen.Open;
        yield return TownScreen.OpenOracle;

        var rituals = new Rituals();
        yield return rituals.Claim();
        yield return rituals.Start();
        NextRunTime = rituals.CurrentRunTime();

        yield return Oracle.Close;
        yield return TownScreen.Close;
    }
}
