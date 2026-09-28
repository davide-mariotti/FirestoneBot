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

    // Lets the last hit's animation finish before the task navigates away. An estimate, not measured.
    private static readonly WaitForSeconds HitResultWait = new(1.5f);

    /// <summary>Selects the first bulk hit multiplier (x10 or x5) the toggle reaches, if it has one.</summary>
    public static IEnumerator TrySetBestQuantity() => QuantityToggle.CycleUntil(
        new GameButton(Paths.ChaosRiftLoc.ChangeHitQuantityBtn), new GameText(Paths.ChaosRiftLoc.HitQuantityTxt),
        QuantityToggle.IsBulk);

    private static GameElement AutoHitToggleElement => new(Paths.ChaosRiftLoc.AutoHitToggleBtn);

    /// <summary>Turns auto-hit on. It's a plain Button today; the Toggle branch covers a future change.</summary>
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

    public static IEnumerator WaitForHitResult()
    {
        yield return HitResultWait;
    }

    public static IEnumerator OpenShop => new GameButton(Paths.ChaosRiftLoc.ShopBtn).Click();

    public static IEnumerator OpenUpgrades => new GameButton(Paths.ChaosRiftLoc.UpgradesBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.ChaosRiftLoc.CloseBtn).Click();
}

public static class ChaosRiftShop
{
    public static bool IsVisible => new GameElement(Paths.ChaosRiftShopLoc.Root).IsVisible();

    // The shop only exists once the "shop" click has landed and its transition finished.
    public static IEnumerator WaitUntilOpen() => Poll.Until(() => IsVisible);

    public static IEnumerator OpenSuppliesTab => new GameButton(Paths.ChaosRiftShopLoc.SuppliesTabBtn).Click();

    public static GameButton TomeOfPowerBuyBtn => new(Paths.ChaosRiftShopLoc.TomeOfPowerBuyBtn);

    public static IEnumerator Close => new GameButton(Paths.ChaosRiftShopLoc.CloseBtn).Click();
}
