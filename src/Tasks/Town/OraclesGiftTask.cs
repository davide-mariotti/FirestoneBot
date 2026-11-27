using System;
using System.Collections;
using System.Linq;
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

    // The badge stays lit after the claim (since 22/10) and the countdown can read empty, which made
    // this run every 2 minutes (14 runs in 5 h on Steam-27, 26/11). The game's last claim time says it.
    // The last claim time can also read before today with the gift already taken (Steam-27, 27/11 from
    // 03:25: a run every 30 min, gifts 6 -> 6), so it's one look per game day; NextRunTime the next.
    public override bool IsNotificationVisible() => GameState.OraclesGiftClaimable is { } claimable
        ? IsEnabled && MeetsLevelRequirement && claimable && !RanThisGameDay
        : base.IsNotificationVisible();

    private bool RanThisGameDay => LastRunTime is { } last && GameState.NextGameDay is { } next && last > next.AddDays(-1);

    public override IEnumerator Execute()
    {
        yield return Notifications.OraclesGift;

        yield return TownScreen.Open;
        yield return TownScreen.OpenOracle;
        yield return Oracle.OpenStore;
        yield return OracleStore.OpenGiftTab;
        yield return Poll.Until(() => OracleStore.IsGiftShown);

        var before = OracleStore.GiftCount;
        if (!OracleStore.IsClaimed)
        {
            yield return OracleStore.ClaimGift;
            // The counter catches up seconds after the click (10/10: 0 -> 0 read at once, 1 later).
            yield return Poll.Until(() => OracleStore.GiftCount > before, 20);
        }

        // The gift renews at the game's daily reset. The countdown can read empty as the store shows up
        // (10/10: renews 01/01/0001), so it's only the fallback.
        if (GameState.NextGameDay is { } reset)
        {
            NextRunTime = reset;
            GameState.CompareTimes("oracle's gift renew", reset, OracleStore.NextRunTime);
        }
        else
        {
            yield return Poll.Until(() => OracleStore.NextRunTime != DateTime.MinValue);
            NextRunTime = OracleStore.NextRunTime;
        }
        Debug($"[INFO] Oracle's gift: gifts {before} -> {OracleStore.GiftCount}, renews {NextRunTime:g}.");

        // Since 22/10 the badge stays lit after the claim. Which tab keeps it lit, and whether the
        // daily tab's own oraclesGift is a second free gift, is still to be seen live.
        var lit = OracleStore.LitTabs;
        Debug($"[INFO] Oracle's store: lit tabs [{string.Join(", ", lit)}].");
        if (lit.Contains("valueBundlesDaily"))
        {
            yield return OracleStore.OpenDailyTab;
            yield return Poll.Until(() => OracleStore.IsDailyGiftClaimable, 6);
            if (OracleStore.IsDailyGiftClaimable)
            {
                var daily = OracleStore.GiftCount;
                yield return OracleStore.ClaimDailyGift;
                yield return Poll.Until(() => OracleStore.GiftCount > daily, 20);
                Debug($"[INFO] Oracle's store: daily tab gift, gifts {daily} -> {OracleStore.GiftCount}.");
            }
        }

        yield return OracleStore.Close;
        yield return Oracle.Close;
        yield return TownScreen.Close;
    }
}
