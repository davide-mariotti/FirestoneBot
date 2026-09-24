using System.Collections;
using Firebot.BotActions;
using Firebot.Core;
using MelonLoader;
using UnityEngine;
using Logger = Firebot.Core.Logger;
using Main = Firebot.Main;

[assembly: MelonInfo(typeof(Main), "Firebot", "0.1.0", "davide-mariotti", "https://github.com/davide-mariotti/firestoneBot")]
[assembly: MelonGame]

[assembly: MelonColor(255, 255, 0, 255)]
[assembly: MelonAuthorColor(255, 0, 255, 0)]

namespace Firebot;

/// <summary>
///     Firebot (see PLAN.md for the task list and design notes). The build does NOT deploy to the
///     game's live Mods folder (see Directory.Build.props) so working on it can't silently overwrite
///     the dll that's actually running on the production bots - copy it over by hand once a change
///     is ready to go live.
/// </summary>
public class Main : MelonMod
{
    private bool _isGameReady;

    public override void OnInitializeMelon()
    {
        BotSettings.Initialize();
        BotManager.Initialize();
        HeroUpgrade.Initialize();
        AutoRetreat.Initialize();
        FlyingBonusHunter.Initialize();

        Logger.Info("Firebot System Initialized.");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        _isGameReady = sceneName == "mainScene" || sceneName == "Game";

        if (_isGameReady)
        {
            // Re-applied here (not just once in OnInitializeMelon, before any scene existed) because
            // the game's own scene load reapplies its saved quality/vSync preference, silently
            // overriding whatever was set before the scene loaded.
            BotSettings.ApplyLowResourceModeOnce();
            if (BotSettings.AutoStart) BotManager.Start();

            // Temporary diagnostic (2026-09-24): the user reports an "offline progress" popup and an
            // event "what's new" popup staying open across a fresh boot, surviving Watchdog.ForceClearAll
            // - logs what's actually active under events/popups/menus a few times as the scene settles,
            // to find their real screen names/roots since neither is currently mapped anywhere in the
            // codebase. Remove once found.
            MelonCoroutines.Start(DumpActiveScreensOverTime());
        }
        else BotManager.Stop();
    }

    private static IEnumerator DumpActiveScreensOverTime()
    {
        foreach (var delay in new[] { 2f, 5f, 10f, 20f })
        {
            yield return new WaitForSeconds(delay);
            Logger.Info($"[StartupPopupDiag] +{delay}s active screens: {Watchdog.DumpActiveScreens()}");
        }
    }

    public override void OnUpdate()
    {
        // Live-confirmed, 2026-09-18 (this session, then corroborated by an actual prior measured
        // attempt at this exact problem): the host game keeps resetting vSyncCount/targetFrameRate
        // on its own well beyond just scene load - a one-shot or timed-window reapply isn't durable.
        // This is two cheap property writes with no logging (see ReassertFrameRateCap) - safe to run
        // unconditionally every frame, and that unconditional-forever reassertion is exactly what the
        // prior attempt measured taking CPU/instance from ~125% down to ~18-25%.
        BotSettings.ReassertFrameRateCap();

        if (_isGameReady && Input.GetKeyDown(BotSettings.ShortcutKey))
        {
            if (BotManager.IsRunning) BotManager.Stop();
            else BotManager.Start();
        }
    }
}
