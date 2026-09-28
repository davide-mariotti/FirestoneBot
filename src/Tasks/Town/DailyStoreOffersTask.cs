using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Town;

/// <summary>
///     Claims the Store's daily check-in and its two free mystery boxes - never the paid bundles next
///     to them. Every tab is clicked explicitly, since the Store reopens on whichever was last used.
///     Reaching the Store relies on the CheckIn/MysteryBox badges: the HUD's storeButton opens nothing
///     (see Store.Open). A badge is up exactly when there's something to claim, so this works - but if
///     the badges ever stopped firing, the task would go silently dead.
/// </summary>
public class DailyStoreOffersTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    protected override string[] NotificationPaths =>
        RailBadges(Paths.BattleLoc.NotificationsLoc.CheckIn, Paths.BattleLoc.NotificationsLoc.MysteryBox);

    private static readonly TimeSpan FallbackRetryDelay = TimeSpan.FromMinutes(30);

    public override IEnumerator Execute()
    {
        yield return Notifications.CheckIn;
        yield return Notifications.MysteryBox;

        yield return Store.Open;

        yield return Store.OpenDailyRewardsTab;
        yield return Store.ClaimCheckIn;
        var checkInNext = Store.CheckInNextRunTime;

        yield return Store.OpenValueBundleDailyTab;
        yield return Store.ClaimFreeMysteryBox;
        var mysteryBoxNext = Store.ValueBundleDailyRenewTime;

        // A separate tab with its own free box, claimed independently of the one above.
        yield return Store.OpenExtremeValueBundleTab;
        yield return Store.ClaimFreeMysteryBoxExtreme;

        yield return Store.Close;

        NextRunTime = EarliestValid(checkInNext, mysteryBoxNext) ?? DateTime.Now + FallbackRetryDelay;
    }

    /// <summary>The soonest time still in the future; unparsed times (DateTime.MinValue) are ignored.</summary>
    private static DateTime? EarliestValid(params DateTime[] times)
    {
        DateTime? earliest = null;
        foreach (var t in times)
        {
            if (t <= DateTime.Now) continue;
            if (earliest == null || t < earliest) earliest = t;
        }

        return earliest;
    }
}
