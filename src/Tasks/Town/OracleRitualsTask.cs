using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Oracle;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Collects finished Oracle rituals and clicks every ritual's start button, in grid order. Only one
///     ritual runs at a time (starting one disables the others' start), so which one starts isn't chosen;
///     the next run is due when it ends. This also covers the daily quest for 2 rituals at level 200.
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
        yield return Oracle.OpenRituals;

        var rituals = new Rituals();
        yield return Poll.Until(rituals.IsVisible);
        yield return rituals.Claim();
        yield return rituals.Start();
        NextRunTime = rituals.CurrentRunTime();
        Debug($"[INFO] Oracle rituals: claimed {rituals.Claimed}, started {rituals.Started}, next run {NextRunTime:g}.");

        yield return Oracle.Close;
        yield return TownScreen.Close;
    }
}
