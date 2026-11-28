using System;
using System.Collections;
using Firebot.Core;
using Firebot.GameModel.Features.Battle;
using Firebot.GameModel.Primitives;
using Firebot.Utilities;
using MelonLoader;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.BotActions;

/// <summary>
///     When the stage stops advancing for stall_minutes, fights the boss if the game is waiting for
///     that; a stage that still doesn't move is a wall the team can't clear, so it farms the stage
///     that pays most gold a minute (measured, at most retreat_stages down), and climbs back to retry
///     the wall every retry_minutes until the next Empower.
/// </summary>
public static class AutoRetreat
{
    private const float PollSeconds = 30f;
    private const float RetreatClickDelaySeconds = 0.5f;
    private static readonly WaitForSeconds PollWait = new(PollSeconds);
    private static readonly WaitForSeconds RetreatClickDelayWait = new(RetreatClickDelaySeconds);

    private static bool _isRunning;
    private static bool _isInitialized;
    private static object _routineHandle;
    private static MelonPreferences_Entry<bool> _isEnabled;
    private static MelonPreferences_Entry<float> _stallMinutes;
    private static MelonPreferences_Entry<int> _retreatStages;

    private static int _lastSeenStage = -1;
    private static DateTime _lastProgressTime = DateTime.MinValue;

    // A lost boss waits for its button however strong the team gets: Steam-7 sat on stage 75 for two
    // days (03-05/11) with heroes that had reached 934. One try per stall, then it's a real wall.
    private static int _bossTriedOn = -1;

    // After one retreat, stay put until the next Empower instead of retreating further on every poll.
    // Saved, or every restart stepped back 5 more: Steam-17 went 285 -> 264 over 17-18/10.
    private static MelonPreferences_Entry<bool> _wallHit;

    private static bool _suppressedUntilReset
    {
        get => _wallHit?.Value ?? false;
        set
        {
            _wallHit.Value = value;
            _wallHit.Category.SaveToFile();
        }
    }

    public static bool IsEnabled => _isEnabled?.Value ?? false;

    /// <summary>Whether this adventure has reached its wall: a stall made it step back (EmpowerTask waits for it).</summary>
    public static bool HitWall => _suppressedUntilReset;
    private static float StallMinutes => Mathf.Clamp(_stallMinutes?.Value ?? 3f, 1f, 180f);
    private static int RetreatStages => Mathf.Clamp(_retreatStages?.Value ?? 5, 1, 50);
    private static float RetryMinutes => Mathf.Clamp(_retryMinutes?.Value ?? 30f, 0f, 1440f);

    private static MelonPreferences_Entry<float> _retryMinutes;
    private static DateTime _retreatTime = DateTime.MinValue;

    // Covers stages lost to retreats from before the flag was saved (Steam-17: 21 under its wall).
    private const int MaxClimbClicks = 60;

    public static void Initialize()
    {
        if (_isInitialized) return;

        var clazzName = StringUtils.Humanize(nameof(AutoRetreat));
        var sectionId = clazzName.Replace(" ", "_").ToLowerInvariant();

        var section = MelonPreferences.CreateCategory(sectionId, $"{clazzName} Settings");
        section.SetFilePath(BotSettings.ConfigPath);

        _isEnabled = section.CreateEntry(
            "enabled",
            true,
            "Enable AutoRetreat",
            "- - - - - - - - - - - - - - - - - - - - - - - - - -"
        );

        _stallMinutes = section.CreateEntry(
            "stall_minutes",
            3f,
            "Stall Threshold (minutes)",
            "When the current stage stops advancing for this long (a difficulty wall), looks for the farm stage " +
            "that pays most gold a minute (up to retreat_stages down), then retries the wall every retry_minutes. " +
            "Clamped between 1 and 180 minutes. Default: 3."
        );

        _retreatStages = section.CreateEntry(
            "retreat_stages",
            5,
            "Retreat Stages",
            "At a wall the bot measures the gold a minute at the wall and one stage down at a time, while that " +
            "pays more, and farms the best: this is how far down it goes at most. Clamped between 1 and 50. Default: 5."
        );

        _retryMinutes = section.CreateEntry(
            "retry_minutes",
            30f,
            "Retry Wall (minutes)",
            "After a retreat, climbs back with the 'go forth stage' arrow every this many minutes to retry " +
            "the wall: the team keeps getting stronger while it farms, and the game doesn't climb back by " +
            "itself. A wall that still holds is retreated from again. 0 = stay until the next Empower. " +
            "Clamped between 0 and 1440. Default: 30."
        );

        _wallHit = section.CreateEntry("wall_hit_internal", false, "Wall Hit (internal)",
            "(auto-managed, don't edit) - set once this adventure has retreated from its wall, cleared by the Empower.");

        section.SaveToFile();
        _isInitialized = true;
        Logger.Info("AutoRetreat configuration initialized.");
    }

    public static void Start()
    {
        if (!IsEnabled) return;
        if (_isRunning) return;
        _isRunning = true;
        _lastSeenStage = -1;
        _lastProgressTime = DateTime.MinValue;
        _retreatTime = DateTime.Now;
        _routineHandle = MelonCoroutines.Start(RetreatLoop());
        Logger.Info("AutoRetreat started.");
    }

    public static void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        if (_routineHandle != null) MelonCoroutines.Stop(_routineHandle);
        _routineHandle = null;
        Logger.Info("AutoRetreat stopped.");
    }

    /// <summary>Called by EmpowerTask right after a successful empower so stall detection re-arms.</summary>
    public static void OnAdventureReset()
    {
        // Always: an Empower during a wall retry (the flag off) must not leave the old run's wall time.
        WallSince = null;
        if (!_suppressedUntilReset) return;

        _suppressedUntilReset = false;
        _lastSeenStage = -1;
        _lastProgressTime = DateTime.MinValue;
        Logger.Info("[AutoRetreat] Adventure reset detected. Stall detection re-armed.");
    }

    private static IEnumerator RetreatLoop()
    {
        while (_isRunning)
        {
            if (BotManager.ShouldPauseBackgroundTasks())
            {
                yield return PollWait;
                continue;
            }

            var stage = StageProgress.Current;

            if (_suppressedUntilReset)
            {
                if (stage >= 0 && RetryMinutes > 0 && DateTime.Now - _retreatTime >= TimeSpan.FromMinutes(RetryMinutes))
                    yield return ClimbBack(stage);

                yield return PollWait;
                continue;
            }

            // Not on the battle screen (a Town errand is running): not a stage change.
            if (stage < 0)
            {
                yield return PollWait;
                continue;
            }

            if (_lastSeenStage < 0 || stage != _lastSeenStage)
            {
                _lastSeenStage = stage;
                _lastProgressTime = DateTime.Now;
            }
            else if (DateTime.Now - _lastProgressTime >= TimeSpan.FromMinutes(StallMinutes))
            {
                if (_bossTriedOn != stage && StageProgress.FightBossBtn.IsClickable())
                {
                    Logger.Info($"[AutoRetreat] Stage {stage} stalled for {StallMinutes:0.#} min on a boss: fighting it.");
                    _bossTriedOn = stage;
                    _lastProgressTime = DateTime.Now;
                    yield return StageProgress.FightBossBtn.Click();
                    yield return PollWait;
                    continue;
                }

                Logger.Info($"[AutoRetreat] Stage {stage} stalled for {StallMinutes:0.#} min: looking for the farm stage that pays most.");
                if (WallSince == null) WallSince = DateTime.Now;
                _suppressedUntilReset = true;
                yield return FarmBest(stage);

                _retreatTime = DateTime.Now;
                _bossTriedOn = -1;
            }

            yield return PollWait;
        }
    }

    /// <summary>A farm search is measuring (EmpowerTask's growth rule waits for it).</summary>
    public static bool FarmSearching { get; private set; }

    /// <summary>When this adventure first stalled at its wall; null before (and after the Empower).</summary>
    public static DateTime? WallSince { get; private set; }

    private const float MeasureSeconds = 120f;

    /// <summary>
    ///     What the stage being farmed pays a minute over MeasureSeconds, log10: the run's gold. The found
    ///     Firestones come from it, about as its 0.4th power, so their own pace falls sample after sample
    ///     for the same gold a minute (-28 28/11 03:40-03:48: gold a minute up every stage down to 1008,
    ///     found a minute peaking at 1010-1009): the gold compares stages, the found only when the gold
    ///     doesn't move. Null when neither moves or the run was reset.
    /// </summary>
    private static IEnumerator MeasureGold(Action<double?> done)
    {
        var (found0, gold0) = (Firebot.GameModel.Shared.GameState.FirestonesFoundLog10, StageProgress.RunGoldLog10);
        var since = DateTime.Now;
        yield return new WaitForSeconds(MeasureSeconds);
        var (found1, gold1) = (Firebot.GameModel.Shared.GameState.FirestonesFoundLog10, StageProgress.RunGoldLog10);
        var minutes = (DateTime.Now - since).TotalMinutes;
        var foundRate = PerMinute(found0, found1, minutes);
        var goldRate = PerMinute(gold0, gold1, minutes);
        Logger.Info($"[AutoRetreat] Farm sample at stage {StageProgress.Current}: found {Rate(foundRate)}, gold {Rate(goldRate)}.");
        _measuredByFound = goldRate == null;
        done(!_suppressedUntilReset ? null : goldRate ?? foundRate);
    }

    private static bool _measuredByFound;

    private static double? PerMinute(double? from, double? to, double minutes) =>
        from is { } a && to is { } b && b > a && minutes > 0
            ? a + Math.Log10(Math.Pow(10, Math.Min(b - a, 300)) - 1) - Math.Log10(minutes)
            : null;

    private static string Rate(double? log10) => log10 is { } r ? $"10^{r:0.00}/min" : "not moving";

    /// <summary>
    ///     The farm stage that pays most, measured: the gold a minute at the wall (the game farms its
    ///     last wave there), then one stage down at a time while that pays more, at most retreat_stages
    ///     down, back up to the best. A higher stage's waves give more gold but take longer to clear, so
    ///     it's the measured pace that picks (the user reads the same off the Barrel of Gold's tooltip;
    ///     the game's own CalculateInventoryItemGold isn't a plain read). Before 28/11 it went a fixed 5
    ///     stages down.
    /// </summary>
    private static IEnumerator FarmBest(int wall)
    {
        // ponytail: one 2-minute sample per stage, so a dragon's gold in it can tip a close call; two samples if that shows up.
        FarmSearching = true;
        double? rate = null;
        yield return MeasureGold(r => rate = r);
        var (bestStage, bestRate) = (StageProgress.Current, rate);
        Logger.Info($"[AutoRetreat] Farm search: stage {bestStage} {Rate(rate)}.");

        for (var step = 0; step < RetreatStages && _suppressedUntilReset && bestRate != null; step++)
        {
            var from = StageProgress.Current;
            // The arrow is off for a moment while the stage changes (2 of 5 clicks ignored, -27 and -28 27/11).
            yield return Poll.Until(() => StageProgress.GoBackBtn.IsClickable(), 20, 0.25f);
            yield return StageProgress.GoBack;
            yield return Poll.Until(() => StageProgress.Current < from, 20, 0.25f);
            var at = StageProgress.Current;
            if (at < 0 || at >= from) break;

            yield return MeasureGold(r => rate = r);
            Logger.Info($"[AutoRetreat] Farm search: stage {at} {Rate(rate)}.");
            if (rate == null || rate <= bestRate) break;
            (bestStage, bestRate) = (at, rate);
        }

        for (var i = 0; i < MaxClimbClicks && _suppressedUntilReset && StageProgress.Current is var now && now >= 0 && now < bestStage; i++)
        {
            // The forth arrow hides for a while during a wave: 5 s weren't enough on -28 (28/11 03:48, stayed 1008 for 1009).
            yield return Poll.Until(() => StageProgress.GoForthBtn.IsClickable(), 240, 0.25f);
            if (!StageProgress.GoForthBtn.IsClickable()) break;
            yield return StageProgress.GoForthBtn.Click();
            yield return Poll.Until(() => StageProgress.Current > now, 20, 0.25f);
        }

        Logger.Info($"[AutoRetreat] Farming stage {StageProgress.Current}, the best measured {bestStage} " +
                    $"({Rate(bestRate)} {(_measuredByFound ? "found" : "gold")}), wall {wall}.");
        FarmSearching = false;
    }

    // Forward until the stage stops changing (the furthest stage reached), then stall detection
    // decides again whether it's still a wall.
    private static IEnumerator ClimbBack(int from)
    {
        var stage = from;
        for (var i = 0; i < MaxClimbClicks && StageProgress.GoForthBtn.IsClickable(); i++)
        {
            yield return StageProgress.GoForthBtn.Click();
            yield return RetreatClickDelayWait;

            var now = StageProgress.Current;
            if (now <= stage) break;
            stage = now;
        }

        Logger.Info($"[AutoRetreat] Retrying the wall after {RetryMinutes:0.#} min: stage {from} -> {stage}.");
        _suppressedUntilReset = false;
        _lastSeenStage = -1;
    }
}
