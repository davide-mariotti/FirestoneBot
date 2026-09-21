using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;
using UnityEngine.UI;
using static Firebot.Core.BotSettings;

namespace Firebot.GameModel.Features.Guild;

public static class ChaosRift
{
    public static bool IsVisible => new GameElement(Paths.ChaosRiftLoc.Root).IsVisible();

    public static GameButton HitBtn => new(Paths.ChaosRiftLoc.HitBtn);

    private static GameElement AutoHitToggleElement => new(Paths.ChaosRiftLoc.AutoHitToggleBtn);

    /// <summary>
    ///     Enables the Auto-hit toggle if it's currently off. autoHitToggle is a plain Button, not a
    ///     Toggle component despite the name - the Toggle branch below is kept as a fallback in case
    ///     that ever changes.
    /// </summary>
    public static IEnumerator EnsureAutoHitOn()
    {
        if (AutoHitToggleElement.TryGetComponent<Toggle>(out var toggle))
        {
            if (!toggle.isOn) toggle.isOn = true;
        }
        else if (AutoHitToggleElement.TryGetComponent<Button>(out var button))
        {
            if (button.enabled && button.interactable) button.onClick.Invoke();
        }

        yield return new WaitForSeconds(InteractionDelay);
    }

    public static IEnumerator OpenShop => new GameButton(Paths.ChaosRiftLoc.ShopBtn).Click();

    public static IEnumerator OpenUpgrades => new GameButton(Paths.ChaosRiftLoc.UpgradesBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.ChaosRiftLoc.CloseBtn).Click();
}

public static class ChaosRiftShop
{
    // Live-confirmed, 2026-09-20: unlike every other screen in this codebase (permanently present,
    // just inactive), this one only comes into existence as a real GameObject the first time "shop"
    // is successfully clicked - every path under it (including this root) reads as "path broken", not
    // just "hidden", until that first click lands and the transition finishes. A first live run tried
    // to interact with it with no wait at all and got exactly that "path broken" wall; polling this
    // instead of guessing a fixed delay, same principle as ScarabGame.WaitUntilClickable.
    public static bool IsVisible => new GameElement(Paths.ChaosRiftShopLoc.Root).IsVisible();

    private static readonly WaitForSeconds OpenPollWait = new(0.5f);
    private const int MaxOpenPolls = 10;

    public static IEnumerator WaitUntilOpen()
    {
        var pollsLeft = MaxOpenPolls;
        while (pollsLeft > 0 && !IsVisible)
        {
            yield return OpenPollWait;
            pollsLeft--;
        }
    }

    public static IEnumerator OpenSuppliesTab => new GameButton(Paths.ChaosRiftShopLoc.SuppliesTabBtn).Click();

    // Per the user (2026-09-20): ONLY Tome of Power is bought here - Eclipse Stone must never be
    // purchased, an explicit standing instruction (see ChaosRiftLoc.MoonstoneOneBuyBtn/BulkBuyBtn,
    // kept defined but deliberately never wired to anything).
    public static GameButton TomeOfPowerBuyBtn => new(Paths.ChaosRiftShopLoc.TomeOfPowerBuyBtn);

    public static IEnumerator Close => new GameButton(Paths.ChaosRiftShopLoc.CloseBtn).Click();
}
