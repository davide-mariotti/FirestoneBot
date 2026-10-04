using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;

namespace Firebot.Tasks.Town;

/// <summary>
///     Claims the Store's daily check-in and its two free mystery boxes - never the paid bundles next
///     to them. Every tab is clicked explicitly, since the Store reopens on whichever was last used.
///     Reaching the Store relies on the CheckIn/MysteryBox badges: the HUD's storeButton opens nothing
///     (see Store.Open). Once today's check-in is claimed the task waits for the 10:00 reset; until
///     then it retries every 30 minutes. The badges bring it back for anything that renews in between.
/// </summary>
public class DailyStoreOffersTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    protected override string[] NotificationPaths =>
        RailBadges(Paths.BattleLoc.NotificationsLoc.CheckIn, Paths.BattleLoc.NotificationsLoc.MysteryBox);

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(30);

    private MelonPreferences_Entry<string> _lastDoneDate;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_lastDoneDate != null) return;

        _lastDoneDate = category.CreateEntry(
            "last_done_date",
            "",
            "Last Done Date",
            "(auto-managed, don't edit) - the last game-day the check-in was claimed. Until the next reset " +
            "at 10:00 only the badges bring the task back."
        );
    }

    public override IEnumerator Execute()
    {
        yield return Notifications.CheckIn;
        yield return Notifications.MysteryBox;

        yield return Store.Open;

        yield return Store.OpenDailyRewardsTab;
        yield return Store.ClaimCheckIn;

        // The countdown to the next check-in only reads when the Store really opened, right after the
        // claim above - so a valid one means today's check-in is done.
        var nextCheckIn = Store.CheckInNextRunTime;
        var checkInClaimed = nextCheckIn > DateTime.Now;

        yield return Store.OpenValueBundleDailyTab;
        yield return Store.ClaimFreeMysteryBox;

        // A separate tab with its own free box, claimed independently of the one above.
        yield return Store.OpenExtremeValueBundleTab;
        yield return Store.ClaimFreeMysteryBoxExtreme;

        yield return Store.Close;

        var today = GameDay.Today();
        if (checkInClaimed && _lastDoneDate != null) _lastDoneDate.Value = today;

        // The countdown, not the date: the badges light at the game's 10:00 reset, and a claim before
        // the bot's 10:02 still counts as yesterday's (04/10: 6 instances retried every 30 minutes).
        NextRunTime = checkInClaimed ? nextCheckIn
            : _lastDoneDate?.Value == today ? GameDay.NextReset() : DateTime.Now + RetryDelay;
    }
}
