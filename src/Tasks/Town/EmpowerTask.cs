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
///     The Temple of Eternals prestige ("Empower"): banks the Firestones found this adventure, which
///     raises the Firestone multiplier, and restarts from stage 1. Free - it's gated on the Firestones
///     found compared to those already banked (min_reset_ratio), plus optional adventure-time limits
///     that are off by default.
/// </summary>
public class EmpowerTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    private MelonPreferences_Entry<float> _minResetRatio;
    private MelonPreferences_Entry<int> _minAdventureMinutes;
    private MelonPreferences_Entry<int> _maxAdventureMinutes;

    // The TemplePrestige badge is only used as a navigation shortcut, not for scheduling: whether it
    // reflects this task's own threshold is unknown.
    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_minResetRatio != null) return;

        _minResetRatio = category.CreateEntry(
            "min_reset_ratio",
            1.0f,
            "Minimum Reset Ratio",
            "Empowers (resets) the Temple of Eternals once the Firestones found in the current adventure " +
            "reach this multiple of the Firestones already banked in the Temple. Default: 1.0 (found >= banked, " +
            "i.e. a +100% gain - the F2P guide's threshold)."
        );

        _minAdventureMinutes = category.CreateEntry(
            "min_adventure_minutes",
            0,
            "Minimum Adventure Minutes",
            "Minimum time (in minutes) that must have passed in the current adventure before the bot " +
            "considers empowering, even if the ratio above is already met. Default: 0 (disabled - only " +
            "the ratio decides)."
        );

        _maxAdventureMinutes = category.CreateEntry(
            "max_adventure_minutes",
            0,
            "Maximum Adventure Minutes",
            "Once the current adventure has run for this many minutes, the bot empowers regardless of " +
            "the ratio above. Set to 0 to disable (only the ratio decides). Default: 0 - a time-based " +
            "override would empower below the ratio, which the F2P guide rules out."
        );
    }

    public override IEnumerator Execute()
    {
        yield return Notifications.TemplePrestige;

        yield return TownScreen.Open;
        yield return TownScreen.OpenTempleOfEternals;

        // The Temple sometimes shows up seconds late: read at once, all three texts came back
        // empty on ~20 of ~630 runs on 30/09 (Steam-10 at 14:19).
        yield return Poll.Until(() => TempleOfEternals.IsShown);
        if (!TempleOfEternals.IsShown)
        {
            Debug("[INFO] Temple of Eternals didn't open within 5 s - retrying in 5 min.");
            yield return TempleOfEternals.Close;
            yield return TownScreen.Close;
            NextRunTime = DateTime.Now + RetryDelay;
            yield break;
        }

        var timePlayed = TimeParser.ParseFrom(TempleOfEternals.AdventureTimePlayedText);
        var found = TempleOfEternals.FirestonesFound;
        var owned = TempleOfEternals.FirestonesYouOwn;

        var ratio = owned > 0 ? found / owned : 0;
        var minRatio = _minResetRatio?.Value ?? 1.0f;
        var minDuration = TimeSpan.FromMinutes(_minAdventureMinutes?.Value ?? 0);
        var maxMinutes = _maxAdventureMinutes?.Value ?? 0;
        var maxDuration = maxMinutes > 0 ? TimeSpan.FromMinutes(maxMinutes) : TimeSpan.MaxValue;

        var foundText = TempleOfEternals.FirestonesFoundText;
        var ownedText = TempleOfEternals.FirestonesYouOwnText;

        Debug($"[INFO] Adventure time: {timePlayed}, Firestones found: {found} ('{foundText}'), " +
              $"Temple's Firestones: {owned} ('{ownedText}'), " +
              $"Ratio: {ratio:0.##} (need {minRatio:0.##} after {minDuration}, or force empower after {maxDuration}).");

        // Only a brand-new account has nothing banked: a 0 here means the text didn't parse, and then
        // the ratio can never trigger an empower.
        if (owned <= 0)
            Debug($"[FAILED] Temple's Firestones read as 0 from '{ownedText}' - no ratio-based empower until fixed.");

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

            // No point checking again before the minimum adventure time has passed - unless nothing read
            // (the Temple never opened, e.g. behind the start-up popups): then its 0 time means nothing,
            // and waiting for it cost an hour on 3 instances after a restart (30/09).
            var timeUntilMinDuration = owned > 0 ? minDuration - timePlayed : TimeSpan.Zero;
            NextRunTime = DateTime.Now + (timeUntilMinDuration > RetryDelay ? timeUntilMinDuration : RetryDelay);
        }
    }
}
