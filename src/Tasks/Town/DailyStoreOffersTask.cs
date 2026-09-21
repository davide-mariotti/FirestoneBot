using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Town;

/// <summary>
///     Claims the daily check-in reward and the free daily mystery box (Store &gt; "Pacchetti
///     Giornalieri" tab) - NOT the paid bundle slots next to it, see Store.ClaimFreeMysteryBox.
///     Covers two separately-badged claims (CheckIn, MysteryBox); NotificationBadgeName only takes
///     one bare name, so this uses NotificationPathCandidates instead (4 full paths: each badge on
///     both HUD rail variants, see NotificationsLoc.Root/FallbackRoot) so either one showing up
///     drives scheduling - live-confirmed real, 2026-09-20 (BotManager's notification rail
///     diagnostic dump), and neither is a threshold gate (a daily claim either exists or doesn't).
///     Always clicks each tab explicitly afterwards rather than assuming whichever one the
///     notification (if any) happened to open is the only one that needs doing - Store remembers
///     the last tab you had open, and both claims need checking every run regardless of entry point.
///     KNOWN BROKEN, 2026-09-20 - Store never actually opens. Root-caused via 8 rounds of live
///     diagnostics (all on Steam-0): storeButton (battleRoot/.../rightSideUI/menuButtons/storeButton)
///     is real, visible=True, and has a genuine enabled+interactable UnityEngine.UI.Button component -
///     but that Button's onClick has 0 listeners (persistent or otherwise), so Click() is a silent
///     no-op. ClickSimulated() (real IPointerDown/Up/ClickHandler events via the UI EventSystem) was
///     also tried, both on storeButton itself and its "collider" child (storeButton's other children:
///     spineStore, name, notification - a Spine-animated icon, strongly suggesting the real click is
///     driven by a physics Collider + OnMouseDown-style script, which is NOT reachable via any
///     IPointerXHandler interface ExecuteEvents can dispatch to). GameNotificationButton (tries a
///     NotificationInteraction component first) was also tried - no such component was found, it fell
///     back to the same inert Button. Every attempt leaves both menus/ and popups/ (confirmed working
///     roots for every other screen automated this session) completely empty afterward - nothing
///     opens at all, no error ever logged. This needs either the real click-handler script's identity
///     (Il2Cpp interop couldn't resolve its type name - showed as generic "Component") or a real
///     OS-level mouse click at the button's actual screen position - flagged for the user rather than
///     guessed at further.
/// </summary>
public class DailyStoreOffersTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    protected override string[] NotificationPathCandidates => new[]
    {
        Paths.BattleLoc.NotificationsLoc.Root + "/" + Paths.BattleLoc.NotificationsLoc.CheckIn,
        Paths.BattleLoc.NotificationsLoc.FallbackRoot + "/" + Paths.BattleLoc.NotificationsLoc.CheckIn,
        Paths.BattleLoc.NotificationsLoc.Root + "/" + Paths.BattleLoc.NotificationsLoc.MysteryBox,
        Paths.BattleLoc.NotificationsLoc.FallbackRoot + "/" + Paths.BattleLoc.NotificationsLoc.MysteryBox
    };

    private static readonly TimeSpan FallbackRetryDelay = TimeSpan.FromMinutes(30);

    public override IEnumerator Execute()
    {
        // Fast path: jump straight in if a badge is already showing. Safe no-ops otherwise.
        yield return Notifications.CheckIn;
        yield return Notifications.MysteryBox;

        yield return Store.Open;

        yield return Store.OpenDailyRewardsTab;
        yield return Store.ClaimCheckIn;
        var checkInNext = Store.CheckInNextRunTime;

        yield return Store.OpenValueBundleDailyTab;
        yield return Store.ClaimFreeMysteryBox;
        var mysteryBoxNext = Store.ValueBundleDailyRenewTime;

        yield return Store.Close;

        NextRunTime = EarliestValid(checkInNext, mysteryBoxNext) ?? DateTime.Now + FallbackRetryDelay;
    }

    /// <summary>Picks the soonest future timestamp, ignoring any that failed to parse (DateTime.MinValue).</summary>
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
