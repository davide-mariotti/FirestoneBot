using System;
using System.Collections;
using Firebot.Core;
using Firebot.GameModel.Features.Battle;
using Firebot.Utilities;
using MelonLoader;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.BotActions;

/// <summary>
///     When the stage stops advancing for stall_minutes (a wall the team can't clear), steps back
///     retreat_stages stages to farm one that clears quickly, and climbs back to retry the wall every
///     retry_minutes until the next Empower.
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
            "When the current stage stops advancing for this long (a difficulty wall), clicks the in-battle " +
            "'go back stage' arrow retreat_stages times to drop to an easier stage that clears quickly, then " +
            "retries the wall every retry_minutes. Clamped between 1 and 180 minutes. Default: 3."
        );

        _retreatStages = section.CreateEntry(
            "retreat_stages",
            5,
            "Retreat Stages",
            "How many stages to step back (one click each) once a wall is detected. Clamped between 1 and 50. Default: 5."
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
                Logger.Info(
                    $"[AutoRetreat] Stage {stage} stalled for {StallMinutes:0.#} min. Retreating {RetreatStages} stage(s).");

                for (var i = 0; i < RetreatStages; i++)
                {
                    yield return StageProgress.GoBack;
                    yield return RetreatClickDelayWait;
                }

                _suppressedUntilReset = true;
                _retreatTime = DateTime.Now;
            }

            yield return PollWait;
        }
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
