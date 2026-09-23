using System.Collections;
using System.Linq;
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

    private static GameButton ChangeHitQuantityBtn => new(Paths.ChaosRiftLoc.ChangeHitQuantityBtn);

    private static GameText HitQuantityTxt => new(Paths.ChaosRiftLoc.HitQuantityTxt);

    // No dedicated result popup to poll for (per the confirmed paths, godParent bundles only VFX/
    // animation children, not a separate screen) - same shape as Awakening's Awaken(), which needed a
    // fixed wait after its own animation for the same reason. This is a rough estimate, not measured
    // frame-by-frame - adjust if it's cutting the hit animation short or needlessly slow once tested
    // live. Applied once after the whole hit loop (not per hit) since the user's concern was
    // specifically about navigating to the shop before the last hit's animation resolves, not about
    // pacing between individual hits.
    private static readonly WaitForSeconds HitResultWait = new(1.5f);

    /// <summary>
    ///     User-requested optimization (2026-09-23), same pattern as ArcaneCrystal.TrySetQuantityTo5 /
    ///     Tavern.TrySetPlayQuantityTo: cycles changeHitQuantity looking for a multiplier so one Hit
    ///     click covers several at once instead of one at a time. The exact values this cycles through
    ///     aren't confirmed live yet, so this tries a list of candidates highest-first and stops at
    ///     the first one found; if none of them turn up within one full cycle, restores the original
    ///     quantity exactly before returning, so a caller falling back to individual Hit clicks isn't
    ///     left at some other multiplier by mistake. Check HitQuantityTxt afterward (or just rely on
    ///     HitBtn.IsClickable() to naturally stop the loop sooner) to know what happened.
    /// </summary>
    private static readonly string[] QuantityCandidatesDescending = { "10", "5" };

    public static IEnumerator TrySetBestQuantity()
    {
        if (QuantityCandidatesDescending.Any(c => HitQuantityTxt.GetParsedText().Contains(c)))
            yield break;

        var original = HitQuantityTxt.GetParsedText();
        var found = false;

        for (var i = 0; i < 6; i++)
        {
            yield return ChangeHitQuantityBtn.Click();

            if (QuantityCandidatesDescending.Any(c => HitQuantityTxt.GetParsedText().Contains(c)))
            {
                found = true;
                break;
            }

            if (HitQuantityTxt.GetParsedText() == original) break; // full loop back - none exist
        }

        if (found) yield break;

        // Same defensive double-check as ArcaneCrystal.TrySetQuantityTo5: in case 6 clicks wasn't a
        // full cycle, keep clicking forward until back at the original value instead of leaving the
        // quantity at some arbitrary non-original setting.
        for (var i = 0; i < 6 && HitQuantityTxt.GetParsedText() != original; i++)
            yield return ChangeHitQuantityBtn.Click();
    }

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

    /// <summary>Waits out the last hit's animation before the caller navigates away (e.g. to the
    /// shop) - see HitResultWait.</summary>
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
