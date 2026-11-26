using System;
using System.Collections;
using Firebot.BotActions;
using Firebot.Core;
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
///     found compared to those already banked (min_reset_ratio) and on the adventure having reached its
///     wall (AutoRetreat), plus optional adventure-time limits that are off by default.
/// </summary>
public class EmpowerTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(5);

    // Up to 10 s for each screen of the Empower.
    private const int PopupPolls = 20;

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

    private static IEnumerator ClickWhenReady(string path, Action<bool> ready)
    {
        var button = new GameButton(path);
        yield return Poll.Until(() => button.IsClickable(), PopupPolls);
        ready(button.IsClickable());
        yield return button.Click();
    }

    /// <summary>
    ///     The empower rule, the same whether the numbers come from the game or off the Temple's screen:
    ///     the ratio found/banked at the wall past the minimum time, or the maximum time. `next` is when to
    ///     look again when it's not time yet.
    /// </summary>
    private bool ShouldEmpower(TimeSpan timePlayed, double found, double owned, out DateTime next, out string why)
    {
        var ownedRead = !double.IsNegativeInfinity(owned);
        var ratio = ownedRead ? Math.Pow(10, found - owned) : 0;
        var minRatio = _minResetRatio?.Value ?? 1.0f;
        var minDuration = TimeSpan.FromMinutes(_minAdventureMinutes?.Value ?? 0);
        var maxMinutes = _maxAdventureMinutes?.Value ?? 0;
        var maxDuration = maxMinutes > 0 ? TimeSpan.FromMinutes(maxMinutes) : TimeSpan.MaxValue;

        // The Firestones found grow with the run's top stage, about x100 every 5 minutes near it: on
        // Steam-0 (17/10) the ratio went 0.12 -> 29 in one check and the empower came while still
        // climbing, short of the 917 record. So the ratio only counts once the climb has stopped.
        var atWall = AutoRetreat.HitWall || !AutoRetreat.IsEnabled;

        // An unreadable time must not hold the empower back: on Steam-17..31 it read 0 for days while
        // the ratio passed 400 and the bot sat 5 stages under its wall (17-18/10).
        var timeKnown = timePlayed > TimeSpan.Zero;

        why = $"ratio {ratio:0.##} (need {minRatio:0.##} after {minDuration} at the wall, or force empower after {maxDuration}), " +
              $"at the wall: {atWall}";

        // No point checking again before the minimum adventure time has passed - unless nothing read
        // (the Temple never opened, e.g. behind the start-up popups): then its 0 time means nothing,
        // and waiting for it cost an hour on 3 instances after a restart (30/09).
        var timeUntilMinDuration = ownedRead ? minDuration - timePlayed : TimeSpan.Zero;
        next = DateTime.Now + (timeUntilMinDuration > RetryDelay ? timeUntilMinDuration : RetryDelay);

        return ((!timeKnown || timePlayed >= minDuration) && ratio >= minRatio && atWall) || timePlayed >= maxDuration;
    }

    public override IEnumerator Execute()
    {
        // The game's own numbers decide without opening the Temple (38 openings in 5 h on Steam-27, 26/11);
        // it only opens to empower, or when the game can't be read. Each check also logs the run's curve,
        // to tune the rule on (GAMESTATE_PLAN.md, "Flussi nuovi" 4).
        var gameTime = GameState.AdventureTime;
        var gameFound = GameState.FirestonesFoundLog10;
        var gameOwned = GameState.FirestonesOwnedLog10;
        var fromGame = gameTime != null && gameFound != null && gameOwned != null;
        if (fromGame)
        {
            var prestigeBadge = GameState.HasBadge(Il2Cpp.NotificationType.TemplePrestige);
            Logger.Info($"[Empower] curve: run {gameTime.Value.TotalMinutes:0} min, found 10^{gameFound:0.##}, " +
                        $"owned 10^{gameOwned:0.##}, stage {AutoRetreat.HitWall switch { true => "at the wall", false => "climbing" }}, " +
                        $"TemplePrestige badge {prestigeBadge?.ToString() ?? "?"}");

            if (!ShouldEmpower(gameTime.Value, gameFound.Value, gameOwned.Value, out var later, out var reason))
            {
                Debug($"[INFO] Not yet, from the game: {reason}.");
                NextRunTime = later;
                yield break;
            }
        }

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

        var timeText = TempleOfEternals.AdventureTimePlayedText;
        var timePlayed = TimeParser.ParseFrom(timeText);
        var found = TempleOfEternals.FirestonesFoundLog10;
        var owned = TempleOfEternals.FirestonesYouOwnLog10;
        var foundText = TempleOfEternals.FirestonesFoundText;
        var ownedText = TempleOfEternals.FirestonesYouOwnText;

        if (fromGame)
        {
            GameState.Compare("firestones found", $"{gameFound:0.#}", $"{found:0.#}");
            GameState.Compare("firestones owned", $"{gameOwned:0.#}", $"{owned:0.#}");
        }

        var empower = ShouldEmpower(timePlayed, found, owned, out var next, out var why);
        Debug($"[INFO] Adventure time: {timePlayed} ('{timeText}'), Firestones found: 10^{found:0.##} ('{foundText}'), " +
              $"Temple's Firestones: 10^{owned:0.##} ('{ownedText}'), {why}.");

        // Only a brand-new account has nothing banked: a 0 here means the text didn't parse, and then
        // the ratio can never trigger an empower.
        if (double.IsNegativeInfinity(owned))
            Debug($"[FAILED] Temple's Firestones read as 0 from '{ownedText}' - no ratio-based empower until fixed.");
        if (timePlayed <= TimeSpan.Zero)
            Debug($"[FAILED] Adventure time read as 0 from '{timeText}' - ignoring the minimum adventure time.");

        if (empower)
        {
            // Each screen can take longer than the click delay, the first time after a start above all:
            // on Steam-0 (05/11) the popup wasn't up yet, nothing was empowered and the wall was forgotten.
            var empowered = false;
            yield return TempleOfEternals.OpenEmpowerPopup;
            yield return ClickWhenReady(Paths.MenusLoc.EmpowerPopupLoc.EmpowerBtn, ok => empowered = ok);
            if (empowered) yield return ClickWhenReady(Paths.MenusLoc.ActionRequiredLoc.ConfirmBtn, ok => empowered = ok);
            if (empowered) yield return ClickWhenReady(Paths.MenusLoc.TOEPrestigeCompleteLoc.ConfirmBtn, ok => empowered = ok);

            if (empowered) AutoRetreat.OnAdventureReset();
            else Debug($"[FAILED] Empower didn't go through. Screens: {Watchdog.DumpActiveScreens()}");

            NextRunTime = DateTime.Now + RetryDelay;
        }
        else
        {
            yield return TempleOfEternals.Close;
            yield return TownScreen.Close;
            NextRunTime = next;
        }
    }
}
