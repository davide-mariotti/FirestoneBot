using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Oracle;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Spends glyphs (from celestial chests) on the Oracle's blessings: one level on every blessing the
///     game marks as affordable, again while the OracleBlessings badge stays lit.
/// </summary>
public class OracleBlessingsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 200;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.OracleBlessings;

    public override IEnumerator Execute()
    {
        yield return Notifications.OracleBlessings;

        yield return TownScreen.Open;
        yield return TownScreen.OpenOracle;
        yield return Oracle.OpenBlessings;

        var blessings = new Blessings();
        yield return Poll.Until(blessings.IsVisible);
        yield return blessings.UpgradeLit();
        Debug($"[INFO] Oracle blessings: upgraded {blessings.Upgraded}.");

        yield return Oracle.Close;
        yield return TownScreen.Close;
    }
}
