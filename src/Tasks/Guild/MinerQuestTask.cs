using System;
using System.Collections;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Logger = Firebot.Core.Logger;

namespace Firebot.Tasks.Guild;

/// <summary>
///     The daily "Miner" quest: 5 hits on the Guild's Arcane Crystal, one pickaxe each at x1.
///     FreePickaxesTask keeps the pickaxes coming; without enough of them the quest waits for the
///     next check.
/// </summary>
public class MinerQuestTask : DailyQuestTask
{
    protected override int MinimumCharacterLevel => 50;

    protected override string QuestName => "Miner";

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ArcaneCrystal;

    // The Arcane Crystal badge stays lit after today's hits are done - it tracks something else on
    // that screen - so it only counts while the quest is still open.
    public override bool IsNotificationVisible() => base.IsNotificationVisible() && !IsDoneToday;

    protected override IEnumerator Work(int missing) => HitCrystal(missing);

    /// <summary>
    ///     Up to `hits` hits, one pickaxe each, stopping at the first that doesn't land. Also an event
    ///     challenge's (EventChallengeActions). onLanded gets how many landed.
    /// </summary>
    public static IEnumerator HitCrystal(int hits, Action<int> onLanded = null)
    {
        yield return Notifications.ArcaneCrystal;

        yield return TownGuild.Open;
        yield return TownGuild.OpenArcaneCrystal;

        yield return ArcaneCrystal.TrySetQuantityTo1();

        var pickaxes = ArcaneCrystal.PickaxeCount;
        Logger.Debug($"[INFO] Arcane Crystal: {hits} hit(s) to do, {pickaxes} pickaxe(s), cost per hit {ArcaneCrystal.HitCost}.");

        var landed = 0;
        var hit = ArcaneCrystal.HitCost == 1;
        for (var i = 0; i < Math.Min(hits, pickaxes) && hit; i++)
        {
            yield return ArcaneCrystal.Hit(ok => hit = ok);
            if (hit) landed++;
        }

        onLanded?.Invoke(landed);

        yield return ArcaneCrystal.Close;
        yield return TownGuild.Close;
    }
}
