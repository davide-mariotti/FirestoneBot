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
///     Watches the battle stage counter. When it stops advancing for a while (a difficulty wall the
///     current team can't clear), steps back a few stages so the bot farms an easier, fast-clearing
///     stage instead of idling against the wall until the next Empower (Temple of Eternals reset).
///     Direct port of the original AutoRetreat - no performance changes needed, it already only polls every
///     30s and does nothing while a scheduled BotTask is executing.
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

    // Once a retreat happens, stay put until the next Empower instead of re-checking every poll and
    // retreating further and further down.
    private static bool _suppressedUntilReset;

    private static bool IsEnabled => _isEnabled?.Value ?? false;
    private static float StallMinutes => Mathf.Clamp(_stallMinutes?.Value ?? 3f, 1f, 180f);
    private static int RetreatStages => Mathf.Clamp(_retreatStages?.Value ?? 5, 1, 50);

    public static void Initialize()
    {
        if (_isInitialized) return;

        var clazzName = StringUtils.Humanize(nameof(AutoRetreat));
        var sectionId = clazzName.Replace(" ", "_").ToLowerInvariant();

        var section = MelonPreferences.CreateCategory(sectionId, $"{clazzName} Settings");
        section.SetFilePath(BotSettings.ConfigPath);

        _isEnabled = section.CreateEntry(
            "enabled",
            false,
            "Enable AutoRetreat",
            "- - - - - - - - - - - - - - - - - - - - - - - - - -"
        );

        _stallMinutes = section.CreateEntry(
            "stall_minutes",
            3f,
            "Stall Threshold (minutes)",
            "When the current stage stops advancing for this long (a difficulty wall), clicks the in-battle " +
            "'go back stage' arrow retreat_stages times to drop to an easier stage that clears quickly, then " +
            "stays put until the next Empower. Clamped between 1 and 180 minutes. Default: 3."
        );

        _retreatStages = section.CreateEntry(
            "retreat_stages",
            5,
            "Retreat Stages",
            "How many stages to step back (one click each) once a wall is detected. Clamped between 1 and 50. Default: 5."
        );

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
            if (BotManager.ShouldPauseBackgroundTasks() || _suppressedUntilReset)
            {
                yield return PollWait;
                continue;
            }

            var stage = StageProgress.Current;

            // Not on the battle screen right now (e.g. a Town errand is running) - keep whatever
            // progress timer we already have instead of treating this as a change.
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
            }

            yield return PollWait;
        }
    }
}
