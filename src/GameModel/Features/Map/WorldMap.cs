using System;
using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Map;

public static class WorldMap
{
    public static bool IsVisible => new GameElement(Paths.WorldMapLoc.CloseBtn).IsVisible();

    private static readonly WaitForSeconds OpenPollWait = new(0.5f);
    private const int MaxOpenPolls = 10; // ~5s
    private const int MaxClickAttempts = 3;

    /// <summary>
    ///     Per the user (2026-09-24): this screen intermittently fails to open on some instances
    ///     (Steam-1) - a live diagnostic that same day found rightSideUI/menuButtons/mapButton present
    ///     and active there, ruling out the per-client HUD-variant issue already fixed elsewhere
    ///     (EventsBtn/PathOfGloryBtn), so this is presumably the same "button technically clickable but
    ///     onClick has no real effect" shape already hit and fixed for Store.Open/EventManager.Open/
    ///     WarfrontDailyMissions.Open - hardened the same way (ClickSimulated + poll + retry) as a
    ///     precaution even though this exact run didn't reproduce a failure.
    /// </summary>
    public static IEnumerator Open => ClickAndConfirm(
        () => new GameButton(Paths.BattleLoc.RightSideUILoc.MapBtn), () => IsVisible, "World Map");

    public static IEnumerator OpenMapMissionsTab =>
        new GameButton(Paths.WorldMapLoc.MapMissionsTabBtn).Click();

    /// <summary>Same hardening as Open above, and for the same reason - reaching the Liberation
    ///     Missions list depends on actually landing on this tab first.</summary>
    public static IEnumerator OpenWarfrontCampaignTab => ClickAndConfirm(
        () => new GameButton(Paths.WorldMapLoc.WarfrontCampaignTabBtn),
        () => new GameElement(Paths.WorldMapLoc.WarfrontLoc.DailyMissionsBtn).IsVisible(),
        "Warfront Campaign tab");

    private static IEnumerator ClickAndConfirm(Func<GameButton> button, Func<bool> opened, string label)
    {
        for (var attempt = 1; attempt <= MaxClickAttempts && !opened(); attempt++)
        {
            yield return button().ClickSimulated();

            var pollsLeft = MaxOpenPolls;
            while (!opened() && pollsLeft > 0)
            {
                yield return OpenPollWait;
                pollsLeft--;
            }

            Logger.Debug($"[WorldMap] Open '{label}' attempt {attempt}/{MaxClickAttempts}: visible={opened()}.");
        }
    }

    public static IEnumerator Close => new GameButton(Paths.WorldMapLoc.CloseBtn).Click();
}
