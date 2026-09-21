using System;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace Firebot.Core;

public static class BotSettings
{
    private static MelonPreferences_Category _category;
    private static MelonPreferences_Entry<bool> _autoStart;
    private static MelonPreferences_Entry<float> _startBotDelay;
    private static MelonPreferences_Entry<float> _scanInterval;
    private static MelonPreferences_Entry<float> _interactionDelay;
    private static MelonPreferences_Entry<float> _maxTaskRuntime;
    private static MelonPreferences_Entry<bool> _debugMode;
    private static MelonPreferences_Entry<KeyCode> _shortcutKey;
    private static MelonPreferences_Entry<float> _freeSpeedupSeconds;
    private static MelonPreferences_Entry<bool> _lowResourceMode;
    private static MelonPreferences_Entry<int> _targetFrameRate;
    private static MelonPreferences_Entry<int> _renderQualityLevel;
    private static MelonPreferences_Entry<int> _windowWidth;
    private static MelonPreferences_Entry<int> _windowHeight;
    private static MelonPreferences_Entry<bool> _windowGridEnabled;
    private static MelonPreferences_Entry<int> _windowGridColumns;

    private static string _configPath;
    public static float FreeSpeedupSeconds => Mathf.Clamp(_freeSpeedupSeconds.Value, 0.0f, 180.0f);

    public static string ConfigPath
    {
        get
        {
            if (string.IsNullOrEmpty(_configPath)) _configPath = Path.Combine("UserData", "FirebotPreferences.cfg");
            return _configPath;
        }
    }

    public static bool AutoStart => _autoStart?.Value ?? false;
    public static float StartBotDelay => Mathf.Clamp(_startBotDelay.Value, 10.0f, 120.0f);
    public static float ScanInterval => Mathf.Clamp(_scanInterval.Value, 5.0f, 3600.0f);
    public static float InteractionDelay => Mathf.Clamp(_interactionDelay.Value, 0.5f, 5.0f);
    public static float MaxTaskRuntime => Mathf.Clamp(_maxTaskRuntime.Value, 10.0f, 3600.0f);
    public static bool DebugMode => _debugMode?.Value ?? false;

    public static KeyCode ShortcutKey =>
        Enum.IsDefined(typeof(KeyCode), _shortcutKey.Value) && _shortcutKey.Value != KeyCode.None
            ? _shortcutKey.Value
            : KeyCode.F7;

    public static void Initialize()
    {
        _category = MelonPreferences.CreateCategory("firebot_settings", "Firebot Settings");
        _category.SetFilePath(ConfigPath);

        _autoStart = _category.CreateEntry("auto_start", false, "Auto Start",
            "Determines if the bot logic should be initialized and started automatically upon game launch.");

        _startBotDelay = _category.CreateEntry("start_bot_delay", 10.0f, "Start Bot Delay",
            "The initial cooldown (in seconds) before the bot begins execution." +
            "\nClamped between 10.0 and 120.0 seconds.");

        _scanInterval = _category.CreateEntry("scan_interval", 5.0f, "Scan Interval",
            "The interval (in seconds) between each BotManager verification cycle." +
            "\nClamped between 5.0 and 3600.0 seconds.");

        _interactionDelay = _category.CreateEntry("interaction_delay", 1.0f, "Interaction Delay",
            "The delay (in seconds) between individual UI interactions (clicks, transitions)." +
            "\nClamped between 0.5 and 5.0 seconds.");

        _maxTaskRuntime = _category.CreateEntry("max_task_runtime", 120.0f, "Max Task Runtime",
            "Maximum time (in seconds) a single task is allowed to run before it is aborted." +
            "\nClamped between 10.0 and 3600.0 seconds.");

        _debugMode = _category.CreateEntry("debug_mode", false, "Enable Debug Mode",
            "Enables verbose logging and StackTrace display in the console for easier bug identification.");

        _shortcutKey = _category.CreateEntry("shortcut_key", KeyCode.F7, "Shortcut Key",
            "The physical key used to manually toggle the bot's execution state during gameplay.");

        _freeSpeedupSeconds = _category.CreateEntry(
            "free_speedup_seconds",
            170.0f,
            "Free Speedup Threshold (seconds)",
            "Some timers in the game can be sped up for free if the remaining time is below this threshold (default: 170 seconds = 2 minutes and 50 seconds). " +
            "The maximum allowed value is 180 seconds (3 minutes). " +
            "Set to 0 to disable free speedup. " +
            "Adjust this value to account for lag or future game changes. " +
            "Affects firestone researches, missions, experiments, and map reset timers. " +
            "If the remaining time is less than or equal to this value, the speedup is free (no gems required)."
        );

        _lowResourceMode = _category.CreateEntry("low_resource_mode", true, "Low Resource Mode",
            "When enabled, caps the game's frame rate and forces the lowest graphics quality level " +
            "at startup. The bot reads game state directly from the Unity scene hierarchy, not from " +
            "rendered pixels, so visual quality/frame rate have no effect on bot functionality - only " +
            "on CPU/GPU load. Recommended when running several simultaneous instances on the same " +
            "machine.");

        _targetFrameRate = _category.CreateEntry("target_frame_rate", 15, "Target Frame Rate",
            "Frame rate cap applied when low_resource_mode is enabled. Clamped between 5 and 60. " +
            "Default: 15.");

        _renderQualityLevel = _category.CreateEntry("render_quality_level", 0, "Render Quality Level",
            "Unity quality level index applied when low_resource_mode is enabled (0 = lowest/fastest). " +
            "Clamped between 0 and 5. Default: 0.");

        _windowWidth = _category.CreateEntry("window_width", 640, "Window Width",
            "The game window's client-area width applied when low_resource_mode is enabled. Default: " +
            "640 (matches the previous fixed size). Only change this together with window_height if " +
            "you also enable window_grid_enabled below - there's no reason to want a bigger window " +
            "while low_resource_mode is on otherwise.");

        _windowHeight = _category.CreateEntry("window_height", 480, "Window Height",
            "The game window's client-area height applied when low_resource_mode is enabled. Default: " +
            "480 (matches the previous fixed size). See window_width.");

        _windowGridEnabled = _category.CreateEntry("window_grid_enabled", false, "Window Grid Enabled",
            "When enabled (and low_resource_mode is also on), moves this instance's window to a fixed " +
            "position in a grid on the primary monitor, so multiple simultaneous instances can all be " +
            "visible at once without manual dragging. Which cell this instance uses is derived from " +
            "the 'Steam-N' number in its own install path (Steam-0, Steam-1, ...), filling the grid " +
            "left-to-right then top-to-bottom. New feature, 2026-09-20 - built and tested on Steam-0 " +
            "only so far, not yet confirmed safe for the rest of the fleet.");

        _windowGridColumns = _category.CreateEntry("window_grid_columns", 5, "Window Grid Columns",
            "How many instances per row when window_grid_enabled is on. Default: 5 - pair with " +
            "window_width=512/window_height=384 for a 5x3 grid of 15 instances on a 2560x1440 " +
            "monitor with no overlap and no distortion of the game's 4:3 aspect ratio (that pairing " +
            "isn't the window_width/window_height field defaults, which stay 640x480 for anyone not " +
            "using grid mode - set them explicitly alongside window_grid_enabled).");

        _category.SaveToFile();
        Logger.Info($"System Initialized. Configuration: {ConfigPath}");

        ApplyLowResourceModeOnce();
    }

    /// <summary>
    ///     Live-confirmed, 2026-09-18 (this session, then corroborated by an actual prior measured
    ///     attempt at this exact problem): applying this ONLY once at startup measurably raised GPU
    ///     usage instead of lowering it (~6% -&gt; ~17% observed) - the host game's own scene load (and,
    ///     per that prior attempt's own measurements, its own logic on further frames/scene loads
    ///     after that) silently resets vSyncCount and/or targetFrameRate back. The prior attempt's
    ///     fix, which measured ~125% CPU/instance down to ~18-25%, split this into a one-time setup
    ///     (this method - quality level, audio, resolution) and a separate CHEAP reassertion of just
    ///     vSyncCount+targetFrameRate called every single frame indefinitely, forever - see
    ///     ReassertFrameRateCap and Main.OnUpdate. Idempotent and safe to call more than once.
    /// </summary>
    public static void ApplyLowResourceModeOnce()
    {
        if (!_lowResourceMode.Value) return;

        // SetQualityLevel applies an entire preset that silently resets vSyncCount as a side effect
        // - call it here, once, then let ReassertFrameRateCap keep vSyncCount/targetFrameRate correct
        // afterward instead of re-running this whole (comparatively expensive) preset switch forever.
        QualitySettings.SetQualityLevel(Mathf.Clamp(_renderQualityLevel.Value, 0, 5), true);
        ReassertFrameRateCap();

        // The bot reads game state from the Unity scene hierarchy, never from audio - muting removes
        // real per-instance mixing/DSP cost with zero effect on bot behavior.
        AudioListener.pause = true;

        // The bot reads game state from the Unity scene hierarchy, never from rendered pixels -
        // shrinking the actual render target cuts real per-instance fill-rate/GPU cost with zero
        // effect on bot behavior. Configurable (window_width/window_height, both default 640x480,
        // matching the old hardcoded call) instead of fixed - see WindowLayout for why: tiling many
        // instances into an on-screen grid needs a smaller, grid-cell-sized window, not a bigger one.
        Screen.SetResolution(Mathf.Max(1, _windowWidth.Value), Mathf.Max(1, _windowHeight.Value), false);

        Logger.Info($"Low resource mode applied: targetFrameRate={Application.targetFrameRate}, " +
                    $"qualityLevel={QualitySettings.GetQualityLevel()}, vSyncCount={QualitySettings.vSyncCount}, " +
                    $"audioPaused={AudioListener.pause}, resolution={Screen.width}x{Screen.height}.");

        if (_windowGridEnabled.Value)
            WindowLayout.ApplyGridPosition(Mathf.Max(1, _windowGridColumns.Value), _windowWidth.Value,
                _windowHeight.Value);
    }

    /// <summary>
    ///     Cheap (two property writes, no logging) - meant to be called every frame, forever, from
    ///     Main.OnUpdate. See ApplyLowResourceModeOnce for why a single one-shot apply isn't durable.
    /// </summary>
    public static void ReassertFrameRateCap()
    {
        if (!_lowResourceMode.Value) return;

        // Must be set (and confirmed 0) before targetFrameRate below - Unity only reads
        // targetFrameRate at all while vSyncCount == 0.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Mathf.Clamp(_targetFrameRate.Value, 5, 60);
    }
}
