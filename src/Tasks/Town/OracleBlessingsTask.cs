using System;
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

    // Without it the scheduler's 2-minute retry ran the task with the badge off (22/10, Steam-0).
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private const int MaxPasses = 10;

    // The game's own word on each blessing (OracleHandler.blessings): how many the glyphs level now.
    private static int? Ready() => GameState.Get("oracle blessings", () =>
    {
        var n = 0;
        foreach (var kv in GameState.H.OracleHandler.blessings)
            if (kv.Value != null && !kv.Value.IsMaxLevel() && (kv.Value.hasAvailableUpgrade || kv.Value.CanLevelUp()))
                n++;
        return n;
    });

    public override IEnumerator Execute()
    {
        // None to level, no Oracle to open.
        var ready = Ready();
        if (!IsNotificationVisible() && ready == 0)
        {
            Debug("[INFO] From the game: no blessing the glyphs pay for - not opening.");
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return Notifications.OracleBlessings;

        yield return TownScreen.Open;
        yield return TownScreen.OpenOracle;
        yield return Oracle.OpenBlessings;

        var blessings = new Blessings();
        yield return Poll.Until(blessings.IsVisible);
        // A pass is one level a lit blessing: again while the game says the glyphs pay for more, instead of
        // the badge's next run 30 minutes later (Steam-18 28/11: 5 at 10:12, the last one at 10:43).
        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var before = blessings.Upgraded;
            yield return blessings.UpgradeLit();
            if (blessings.Upgraded == before || !(Ready() > 0)) break;
        }

        Debug($"[INFO] Oracle blessings: upgraded {blessings.Upgraded}.");
        NextRunTime = DateTime.Now + RecheckDelay;

        yield return Oracle.Close;
        yield return TownScreen.Close;
    }
}
