using System.Collections;
using Firebot.Core;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.BotActions;

/// <summary>
///     Taps the flying bonus objects (2 dragon-with-beer variants, 2 meteorite-hunter variants - see
///     Paths.FlyingBonusHunterLoc) as soon as they cross the battle screen. New feature, 2026-09-20 -
///     the old (pre-rewrite) bot config had a [flying_bonus_hunter] section for this exact thing
///     ("Taps the flying dragon-with-beer and meteorite-hunter bonuses when they cross the screen"),
///     but it was never ported over to this codebase - left as an explicit Open Point (see
///     PLAN.md/TESTING.md) until located live this session via a scene-wide recursive name search
///     (see BotManager's former scout diagnostic, removed once this confirmed the real paths).
///     Structurally a standalone polling loop (like HeroUpgrade/AutoRetreat), not a scheduled BotTask
///     competing for the scheduler's one-task-per-tick slot - these are highly transient (visible only
///     while actually flying across the screen for a few seconds), so this needs to check far more
///     often than the scheduler's typical task cadence (hours) to have any real chance of catching one
///     before it's gone.
///     Single click per target, no follow-up handling - unverified whether that's the whole
///     interaction or whether a claim popup can appear after; watch the log/screen the first few times
///     this actually fires live and extend if a popup shows up unclaimed.
/// </summary>
public static class FlyingBonusHunter
{
    private static bool _isRunning;
    private static bool _isInitialized;
    private static object _routineHandle;
    private static MelonPreferences_Entry<bool> _isEnabled;
    private static MelonPreferences_Entry<float> _pollSeconds;

    private static bool IsEnabled => _isEnabled?.Value ?? false;
    private static WaitForSeconds PollWait => new(Mathf.Clamp(_pollSeconds?.Value ?? 2f, 0.5f, 10f));

    private static readonly GameButton[] Targets =
    {
        new(Paths.FlyingBonusHunterLoc.MeteoriteHunterBtn),
        new(Paths.FlyingBonusHunterLoc.CoworkerMeteoriteHunterBtn),
        new(Paths.FlyingBonusHunterLoc.DragonWithBeerBtn),
        new(Paths.FlyingBonusHunterLoc.FemaleDragonWithBeerBtn)
    };

    public static void Initialize()
    {
        if (_isInitialized) return;

        var clazzName = StringUtils.Humanize(nameof(FlyingBonusHunter));
        // Matches the old (pre-rewrite) config's own section name exactly - see this class's doc
        // comment - rather than the usual Humanize-derived id, so anyone who remembers that name
        // finds the same section here.
        const string sectionId = "flying_bonus_hunter";

        var section = MelonPreferences.CreateCategory(sectionId, $"{clazzName} Settings");
        section.SetFilePath(BotSettings.ConfigPath);

        _isEnabled = section.CreateEntry(
            "enabled",
            false,
            "Enable Flying Bonus Hunter",
            "Taps the flying dragon-with-beer and meteorite-hunter bonuses when they cross the screen."
        );

        _pollSeconds = section.CreateEntry(
            "poll_seconds",
            2f,
            "Poll Interval (seconds)",
            "How often to check whether a flying bonus is currently on screen. These are highly " +
            "transient (visible for only a few seconds), so this is intentionally much more frequent " +
            "than other background checks. Clamped between 0.5 and 10 seconds. Default: 2."
        );

        section.SaveToFile();
        _isInitialized = true;
        Logger.Info("FlyingBonusHunter configuration initialized.");
    }

    public static void Start()
    {
        if (!IsEnabled) return;
        if (_isRunning) return;
        _isRunning = true;
        _routineHandle = MelonCoroutines.Start(HuntLoop());
        Logger.Info("FlyingBonusHunter started.");
    }

    public static void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        if (_routineHandle != null) MelonCoroutines.Stop(_routineHandle);
        _routineHandle = null;
        Logger.Info("FlyingBonusHunter stopped.");
    }

    private static IEnumerator HuntLoop()
    {
        while (_isRunning)
        {
            if (BotManager.ShouldPauseBackgroundTasks())
            {
                yield return PollWait;
                continue;
            }

            foreach (var target in Targets)
                if (target.IsClickable())
                    yield return target.Click();

            yield return PollWait;
        }
    }
}
