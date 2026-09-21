using System;
using System.Collections;
using Firebot.BotActions;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;
using TempleOfEternals = Firebot.GameModel.Features.Town.TempleOfEternals.TempleOfEternals;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     "Empower" = the Temple of Eternals reset/prestige action: banks the Firestones found in the
///     current adventure into the permanent total (raising the firestoneEffect multiplier) and
///     restarts the adventure. Not a temporary buff and not spent with gems/coins - it's a free
///     reset gated on how much progress (Firestones found vs. already banked) would be sacrificed.
/// </summary>
public class EmpowerTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    private MelonPreferences_Entry<float> _minResetRatio;
    private MelonPreferences_Entry<int> _minAdventureMinutes;
    private MelonPreferences_Entry<int> _maxAdventureMinutes;

    // No NotificationPath, deliberately - same reasoning as FreePickaxesTask: this is a threshold
    // gate (ratio + min/max adventure time), so the badge being up (if it even reflects that gate at
    // all - unverified, see Battle.cs) doesn't guarantee the reset is actually due yet. Still clicked
    // opportunistically below as a fast path, just not promoted to notification-priority scheduling.
    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_minResetRatio != null) return;

        _minResetRatio = category.CreateEntry(
            "min_reset_ratio",
            2.0f,
            "Minimum Reset Ratio",
            "Empowers (resets) the Temple of Eternals once the Firestones found in the current adventure " +
            "reach this multiple of the Firestones already banked in the Temple. Default: 2.0 (found >= 2x banked)."
        );

        _minAdventureMinutes = category.CreateEntry(
            "min_adventure_minutes",
            60,
            "Minimum Adventure Minutes",
            "Minimum time (in minutes) that must have passed in the current adventure before the bot " +
            "considers empowering, even if the ratio above is already met. Default: 60."
        );

        _maxAdventureMinutes = category.CreateEntry(
            "max_adventure_minutes",
            120,
            "Maximum Adventure Minutes",
            "Once the current adventure has run for this many minutes, the bot empowers regardless of " +
            "the ratio above. Set to 0 to disable (only the ratio decides). Default: 120."
        );
    }

    public override IEnumerator Execute()
    {
        // Fast path: the notification (when up) opens Temple of Eternals directly. Safe no-op
        // otherwise. Unlike every other task's notification, this one has no prior precedent to
        // cross-check (never used a notification for this feature) - see the path comment.
        yield return Notifications.TemplePrestige;

        // Guaranteed path regardless of the notification - same reasoning as the previous tasks:
        // don't rely on the screen already being open.
        yield return TownScreen.Open;
        yield return TownScreen.OpenTempleOfEternals;

        var timePlayed = TimeParser.ParseFrom(TempleOfEternals.AdventureTimePlayedText);
        var found = TempleOfEternals.FirestonesFound;
        var owned = TempleOfEternals.FirestonesYouOwn;

        var ratio = owned > 0 ? found / owned : 0;
        var minRatio = _minResetRatio?.Value ?? 2.0f;
        var minDuration = TimeSpan.FromMinutes(_minAdventureMinutes?.Value ?? 60);
        var maxMinutes = _maxAdventureMinutes?.Value ?? 120;
        var maxDuration = maxMinutes > 0 ? TimeSpan.FromMinutes(maxMinutes) : TimeSpan.MaxValue;

        Debug($"[INFO] Adventure time: {timePlayed}, Firestones found: {found}, Temple's Firestones: {owned}, " +
              $"Ratio: {ratio:0.##} (need {minRatio:0.##} after {minDuration}, or force empower after {maxDuration}).");

        if ((timePlayed >= minDuration && ratio >= minRatio) || timePlayed >= maxDuration)
        {
            yield return TempleOfEternals.OpenEmpowerPopup;
            yield return new GameButton(Paths.MenusLoc.EmpowerPopupLoc.EmpowerBtn).Click();
            yield return new GameButton(Paths.MenusLoc.ActionRequiredLoc.ConfirmBtn).Click();
            yield return new GameButton(Paths.MenusLoc.TOEPrestigeCompleteLoc.ConfirmBtn).Click();

            AutoRetreat.OnAdventureReset();

            NextRunTime = DateTime.Now + RetryDelay;
        }
        else
        {
            yield return TempleOfEternals.Close;
            yield return TownScreen.Close;

            // If the minimum adventure time hasn't elapsed yet, there is no point checking again
            // before it does - skip straight to the moment it will (plus the ratio not being met
            // yet still falls back to the regular retry delay).
            var timeUntilMinDuration = minDuration - timePlayed;
            NextRunTime = DateTime.Now + (timeUntilMinDuration > RetryDelay ? timeUntilMinDuration : RetryDelay);
        }
    }
}
