using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Oracle;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>Claims the daily Oracle's Gift once the character reaches the level it unlocks at.</summary>
public class OraclesGiftTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 200;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.OraclesGift;

    public override IEnumerator Execute()
    {
        yield return Notifications.OraclesGift;

        yield return TownScreen.Open;
        yield return TownScreen.OpenOracle;
        yield return Oracle.OpenStore;

        // At 10 fps the store shows up 2-3 s after the badge click (10/10: read at once, nothing found).
        yield return Poll.Until(() => OracleStore.IsGiftShown, 20);

        var before = OracleStore.GiftCount;
        if (!OracleStore.IsClaimed)
        {
            yield return OracleStore.ClaimGift;
            // The counter catches up seconds after the click (10/10: 0 -> 0 read at once, 1 later).
            yield return Poll.Until(() => OracleStore.GiftCount > before, 20);
        }

        // The countdown can read empty as the store shows up (10/10: renews 01/01/0001).
        yield return Poll.Until(() => OracleStore.NextRunTime != DateTime.MinValue);
        NextRunTime = OracleStore.NextRunTime;
        Debug($"[INFO] Oracle's gift: gifts {before} -> {OracleStore.GiftCount}, renews {NextRunTime:g}.");

        yield return OracleStore.Close;
        yield return Oracle.Close;
        yield return TownScreen.Close;
    }
}
