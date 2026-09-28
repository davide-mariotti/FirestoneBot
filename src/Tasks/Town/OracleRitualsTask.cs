using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Oracle;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Collects finished Oracle rituals and clicks every visible ritual's start button, in grid order.
///     Per the wiki only one ritual runs at a time, so which one starts isn't chosen. Not run live yet
///     (it needs character level 200).
/// </summary>
public class OracleRitualsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 200;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.OracleRituals;

    public override IEnumerator Execute()
    {
        yield return Notifications.OracleRituals;

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
