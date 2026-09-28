using Firebot.BotActions;
using Firebot.Core;
using MelonLoader;
using UnityEngine;
using Logger = Firebot.Core.Logger;

[assembly: MelonInfo(typeof(Firebot.Main), "Firebot", "0.1.0", "davide-mariotti", "https://github.com/davide-mariotti/firestoneBot")]
[assembly: MelonGame]

[assembly: MelonColor(255, 255, 0, 255)]
[assembly: MelonAuthorColor(255, 0, 255, 0)]

namespace Firebot;

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
            // Loading the scene reapplies the game's own quality/vSync settings over ours.
            BotSettings.ApplyLowResourceModeOnce();
            if (BotSettings.AutoStart) BotManager.Start();
        }
        else BotManager.Stop();
    }

    public override void OnUpdate()
    {
        // The game keeps resetting vSync and the frame-rate cap on its own, so this runs every frame.
        BotSettings.ReassertFrameRateCap();

        if (_isGameReady && Input.GetKeyDown(BotSettings.ShortcutKey))
        {
            if (BotManager.IsRunning) BotManager.Stop();
            else BotManager.Start();
        }
    }
}
