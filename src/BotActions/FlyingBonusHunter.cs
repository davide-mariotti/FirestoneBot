using System.Collections;
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
///     Taps the flying bonuses (two dragons carrying beer, two meteorite hunters) as they cross the
///     battle screen. They're visible for only a few seconds, so this is its own fast polling loop
///     rather than a scheduled task. It's a single click each, and only while the bonus is flying:
///     the Buttons are clickable all the time, and a click on an idle hunter throws inside the game.
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

    private static readonly (GameElement Flying, GameButton Button)[] Targets =
    {
        (new(Paths.FlyingBonusHunterLoc.MeteoriteHunterFlying), new(Paths.FlyingBonusHunterLoc.MeteoriteHunterBtn)),
        (new(Paths.FlyingBonusHunterLoc.CoworkerMeteoriteHunterFlying),
            new(Paths.FlyingBonusHunterLoc.CoworkerMeteoriteHunterBtn)),
        (new(Paths.FlyingBonusHunterLoc.DragonWithBeerFlying), new(Paths.FlyingBonusHunterLoc.DragonWithBeerBtn)),
        (new(Paths.FlyingBonusHunterLoc.FemaleDragonWithBeerFlying),
            new(Paths.FlyingBonusHunterLoc.FemaleDragonWithBeerBtn))
    };

    // Per target: already clicked on its current flight. Cleared once it's off screen.
    private static readonly bool[] ClickedThisFlight = new bool[Targets.Length];

    public static void Initialize()
    {
        if (_isInitialized) return;

        var clazzName = StringUtils.Humanize(nameof(FlyingBonusHunter));
        // Fixed id, not Humanize-derived, to match the section name older configs already use.
        const string sectionId = "flying_bonus_hunter";

        var section = MelonPreferences.CreateCategory(sectionId, $"{clazzName} Settings");
        section.SetFilePath(BotSettings.ConfigPath);

        _isEnabled = section.CreateEntry(
            "enabled",
            true,
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

            for (var i = 0; i < Targets.Length; i++)
            {
                var (flying, button) = Targets[i];
                if (!flying.IsVisible())
                {
                    ClickedThisFlight[i] = false;
                    continue;
                }

                // The first click drops the load (its beerDrop/bagDrop child leaves the flyer, seen live
                // 01/10); the flyer then crosses on empty, which used to draw 2 more clicks a flight.
                if (ClickedThisFlight[i] || !button.IsClickable()) continue;

                Logger.Debug($"[FlyingBonusHunter] Clicking {button.FullPath}.");
                yield return button.Click();
                ClickedThisFlight[i] = true;
            }

            yield return PollWait;
        }
    }
}
