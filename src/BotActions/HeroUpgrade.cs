using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.Core;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.BotActions;

/// <summary>
///     Upgrades the leader and every hero slot during battle. Same behavior as the original AutoUpgrade, with
///     two changes to keep CPU/RAM down when many bots run at once:
///     1. The button list is resolved once (via CachedGameButton, which self-heals if the underlying
///        object is destroyed - see CachedGameButton's null-check) and reused, instead of being
///        rebuilt from scratch every loop iteration.
///     2. One quick hold-pass through every button, then a single configurable sweep interval
///        (default 5s) before the next pass - instead of holding each button for 0.5s with a 0.5s
///        gap, forever, back to back.
/// </summary>
public static class HeroUpgrade
{
    private const float HoldSecondsPerButton = 0.5f;

    private static bool _isRunning;
    private static object _routineHandle;
    private static bool _isInitialized;
    private static string _cachedLevel = string.Empty;
    private static List<CachedGameButton> _buttons;

    private static MelonPreferences_Entry<bool> _isEnabled;
    private static MelonPreferences_Entry<float> _sweepIntervalSeconds;
    private static MelonPreferences_Entry<string> _upgradeTargetSlots;

    private static bool IsEnabled => _isEnabled?.Value ?? false;
    private static WaitForSeconds SweepIntervalWait => new(Mathf.Clamp(_sweepIntervalSeconds?.Value ?? 5f, 1f, 60f));

    public static void Initialize()
    {
        if (_isInitialized) return;

        var clazzName = StringUtils.Humanize(nameof(HeroUpgrade));
        var sectionId = clazzName.Replace(" ", "_").ToLowerInvariant();

        var section = MelonPreferences.CreateCategory(sectionId, $"{clazzName} Settings");
        section.SetFilePath(BotSettings.ConfigPath);

        _isEnabled = section.CreateEntry(
            "enabled",
            false,
            "Enable Hero Upgrade",
            "- - - - - - - - - - - - - - - - - - - - - - - - - -"
        );

        _sweepIntervalSeconds = section.CreateEntry(
            "sweep_interval_seconds",
            5f,
            "Sweep Interval (seconds)",
            "How long to wait between upgrade passes (one pass = try every hero slot once). Doesn't need to be " +
            "fast - gold accumulates slowly. Clamped between 1 and 60 seconds. Default: 5."
        );

        _upgradeTargetSlots = section.CreateEntry(
            "upgrade_target_slots",
            "",
            "Upgrade Target Slots",
            "SLOT IDs ARE ZERO-BASED, 0 to 6 (0 = Leader, 1 to 6 = Heroes). " +
            "Comma-separated list to upgrade only specific slots, e.g. '0,6'. Empty = upgrade all slots."
        );

        section.SaveToFile();
        _isInitialized = true;
        Logger.Info("HeroUpgrade configuration initialized.");
    }

    public static void Start()
    {
        if (!IsEnabled) return;
        if (_isRunning) return;
        _isRunning = true;
        _routineHandle = MelonCoroutines.Start(UpgradeLoop());
        Logger.Info("HeroUpgrade started.");
    }

    public static void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        if (_routineHandle != null) MelonCoroutines.Stop(_routineHandle);
        _routineHandle = null;
        Logger.Info("HeroUpgrade stopped.");
    }

    private static List<CachedGameButton> AllSlotButtons()
    {
        var buttons = new List<CachedGameButton>
        {
            new(Paths.BattleLoc.BottomSideUINewLoc.LeaderPanelLoc.LvlUpBtn)
        };

        var heroSlots = new GameElement(Paths.BattleLoc.BottomSideUINewLoc.HeroSlotsLoc.Root);
        foreach (var slot in heroSlots.GetChildren())
            buttons.Add(new CachedGameButton(Paths.BattleLoc.BottomSideUINewLoc.HeroSlotsLoc.LvlUpBtn, slot));

        return buttons;
    }

    /// <summary>
    ///     Builds the button list once battleRoot actually exists (i.e. once we're in a battle scene),
    ///     then keeps reusing it - CachedGameButton already tolerates the underlying object being
    ///     deactivated/reactivated (Town round-trips) or destroyed (scene reload).
    /// </summary>
    private static List<CachedGameButton> ResolvedButtons()
    {
        if (_buttons != null) return _buttons;

        var candidate = AllSlotButtons();
        // AllSlotButtons() always returns at least the leader slot; only trust it once heroSlots
        // actually resolved to something (otherwise we'd cache an incomplete list from before the
        // battle scene finished loading).
        if (candidate.Count > 1) _buttons = candidate;
        return _buttons;
    }

    private static List<CachedGameButton> FilterBySelectedSlots(List<CachedGameButton> allButtons)
    {
        var selected = GetSelectedTargetSlots();
        if (selected.Length == 0) return allButtons;

        var filtered = new List<CachedGameButton>(selected.Length);
        foreach (var position in selected)
            if (position >= 0 && position < allButtons.Count)
                filtered.Add(allButtons[position]);

        return filtered;
    }

    private static int[] GetSelectedTargetSlots()
    {
        if (_upgradeTargetSlots == null) return Array.Empty<int>();

        var value = _upgradeTargetSlots.Value;
        if (string.IsNullOrWhiteSpace(value)) return Array.Empty<int>();

        var parts = value.Split(',');
        var result = new List<int>(parts.Length);
        var seen = new bool[7];

        foreach (var part in parts)
        {
            var candidate = part.Trim();
            if (!int.TryParse(candidate, out var slot)) continue;
            if (slot < 0 || slot > 6) continue;
            if (seen[slot]) continue;

            seen[slot] = true;
            result.Add(slot);
        }

        return result.Count > 0 ? result.ToArray() : Array.Empty<int>();
    }

    private static bool IsTopTier(string levelText) =>
        levelText.IndexOf("x100", StringComparison.OrdinalIgnoreCase) >= 0 ||
        levelText.IndexOf("MAX", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>
    ///     Cycles the buy-quantity toggle until it reaches x100 or MAX, so each upgrade click spends
    ///     as much gold as possible in one go.
    /// </summary>
    private static IEnumerator SetUpgradeLevel()
    {
        var levelTxt = new GameText(Paths.BattleLoc.BottomSideUINewLoc.ChangeLevelUpModeLoc.Text);
        var toggleBtn = new GameButton(Paths.BattleLoc.BottomSideUINewLoc.ChangeLevelUpModeLoc.Button);

        var currentLevel = levelTxt.GetParsedText();

        if (!string.IsNullOrEmpty(currentLevel) &&
            string.Equals(currentLevel, _cachedLevel, StringComparison.Ordinal))
            yield break;

        if (!string.IsNullOrEmpty(currentLevel) && IsTopTier(currentLevel))
        {
            _cachedLevel = currentLevel;
            yield break;
        }

        const int maxAttempts = 10; // Prevent infinite loop in case of unexpected issues
        for (var attempts = 0; attempts < maxAttempts; attempts++)
        {
            yield return toggleBtn.Click();
            var nextLevel = levelTxt.GetParsedText();

            if (string.IsNullOrEmpty(nextLevel)) break;
            if (IsTopTier(nextLevel))
            {
                currentLevel = nextLevel;
                break;
            }

            // Cycled all the way back to where we started without finding x100/MAX.
            if (string.Equals(nextLevel, currentLevel, StringComparison.Ordinal)) break;

            currentLevel = nextLevel;
        }

        _cachedLevel = currentLevel;
    }

    private static IEnumerator UpgradeLoop()
    {
        while (_isRunning)
        {
            if (BotManager.ShouldPauseBackgroundTasks())
            {
                yield return SweepIntervalWait;
                continue;
            }

            yield return SetUpgradeLevel();

            var allButtons = ResolvedButtons();
            if (allButtons == null)
            {
                // Not in battle yet (or hierarchy not found) - nothing to resolve this pass.
                yield return SweepIntervalWait;
                continue;
            }

            foreach (var button in FilterBySelectedSlots(allButtons))
            {
                if (BotManager.ShouldPauseBackgroundTasks()) break;
                yield return button.HoldButton(HoldSecondsPerButton);
            }

            yield return SweepIntervalWait;
        }
    }
}
