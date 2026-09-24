using System;
using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using UnityEngine;
using UnityEngine.UI;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Events;

/// <summary>
///     "New Player Event"'s real screen - see Paths.AnniversaryShopLoc's own doc comment for why it's
///     named "AnniversaryShop" internally. Only Dailies/Activity/Exchange are wired here, per the
///     user - Avatars/Skins/the real-money Shop tab are deliberately never touched.
/// </summary>
public static class AnniversaryShop
{
    public static bool IsVisible => new GameElement(Paths.AnniversaryShopLoc.Root).IsVisible();

    // Same reasoning as DecoratedHeroesShop.WaitUntilOpen - a hub-to-shop transition can outlast the
    // standard interaction_delay.
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

    public static IEnumerator Close => new GameButton(Paths.AnniversaryShopLoc.CloseBtn).Click();

    public static IEnumerator OpenDailiesTab => new GameButton(Paths.AnniversaryShopLoc.DailiesTabBtn).Click();

    public static IEnumerator OpenActivityTab => new GameButton(Paths.AnniversaryShopLoc.ActivityTabBtn).Click();

    public static IEnumerator OpenExchangeTab => new GameButton(Paths.AnniversaryShopLoc.ExchangeTabBtn).Click();

    /// <summary>
    ///     The single daily check-in button (same shape as Store.ClaimCheckIn) - safe no-op via
    ///     IsClickable() once already claimed today.
    /// </summary>
    public static IEnumerator ClaimDailyCheckIn()
    {
        var btn = new GameButton(Paths.AnniversaryShopLoc.DailiesLoc.CheckInBtn);
        var clickable = btn.IsClickable();
        Logger.Debug($"[AnniversaryShop] ClaimDailyCheckIn: clickable={clickable}.");
        if (clickable) yield return btn.Click();
    }

    /// <summary>
    ///     Claims every currently-ready activity milestone (per the user, 2026-09-24: earned by staying
    ///     online a certain amount of time that day). Live-confirmed, 2026-09-24 (full recursive
    ///     structure dump, Steam-10): the real claim button is nested at unlocked/claimButton, not on
    ///     the milestone card itself (that first guess was live-disproven: "Component &lt;Button&gt;
    ///     missing" on the card) - it's only active once that milestone's reward is actually ready to
    ///     collect (inactive otherwise, including after being claimed).
    ///     Per the user (2026-09-24): only the first few days (1-4) were ever actually being claimed -
    ///     days further down the list (needing a scroll to see on screen) silently never got claimed.
    ///     Root cause, same bug class already found and fixed for Pirate's Prize's tier list (see
    ///     PiratesPrizeTask's own doc comment): only the first 4 milestone siblings get a unique Unity
    ///     name ("milestone (0)".."milestone (3)") - every one after that is a clone sharing the exact
    ///     same name ("milestone (3)(Clone)", confirmed live), and GameElement/GameButton always
    ///     re-resolve a child by NAME via its string path (see GameElement's own "Always resolve from
    ///     Path; do not cache transforms" comment) - so every later milestone's ClaimBtn path always
    ///     re-resolved to the SAME first "(Clone)"-named sibling instead of its own, silently repeating
    ///     one claim attempt instead of reaching the real target. Fixed the same way Pirate's Prize
    ///     was: walk raw Transform children by INDEX (never by re-resolving a shared name) and resolve
    ///     each milestone's own claim button straight from that already-known Transform.
    /// </summary>
    public static IEnumerator ClaimActivityMilestones()
    {
        var milestonesRoot = ResolveRawTransform(Paths.AnniversaryShopLoc.ActivityLoc.MilestonesRoot);

        if (milestonesRoot == null)
        {
            Logger.Debug("[AnniversaryShop] ClaimActivityMilestones: milestones root not found - skipping.");
            yield break;
        }

        Logger.Debug($"[AnniversaryShop] ClaimActivityMilestones: {milestonesRoot.childCount} milestone(s) found.");

        var claimed = 0;
        for (var i = 0; i < milestonesRoot.childCount; i++)
            if (TryClaimMilestone(milestonesRoot.GetChild(i)))
            {
                claimed++;
                Logger.Debug($"[AnniversaryShop] ClaimActivityMilestones: claimed index {i} ('{milestonesRoot.GetChild(i).name}').");
            }

        Logger.Debug($"[AnniversaryShop] ClaimActivityMilestones: claimed {claimed}/{milestonesRoot.childCount}.");
        yield break;
    }

    /// <summary>Resolves a shared Paths.*Loc path string ("rootObjectName/relative/path") straight to
    ///     its Transform - see ClaimActivityMilestones' own doc comment for why this bypasses
    ///     GameElement/GameButton's name-based re-resolution. Same helper shape as
    ///     PiratesPrizeTask.ResolveRawTransform.</summary>
    private static Transform ResolveRawTransform(string path)
    {
        var slashIndex = path.IndexOf('/');
        var rootObject = GameObject.Find(slashIndex == -1 ? path : path[..slashIndex]);
        if (rootObject == null) return null;

        return slashIndex == -1 ? rootObject.transform : rootObject.transform.Find(path[(slashIndex + 1)..]);
    }

    /// <summary>Resolves and clicks THIS specific milestone's claim button via its own already-known
    ///     Transform (never by re-resolving "unlocked/claimButton" as a name-based path, which several
    ///     siblings share past the 4th milestone).</summary>
    private static bool TryClaimMilestone(Transform milestone)
    {
        var unlocked = milestone.Find("unlocked");
        var claimButton = unlocked != null ? unlocked.Find("claimButton") : null;
        if (claimButton == null || !claimButton.gameObject.activeInHierarchy) return false;

        if (!claimButton.TryGetComponent<Button>(out var button) || !button.enabled || !button.interactable)
            return false;

        // Plain onClick.Invoke() - live-confirmed working this way for milestone 0 before this fix,
        // unlike Pirate's Prize's tier claim button which needed a simulated ExecuteEvents click.
        button.onClick.Invoke();
        return true;
    }

    private static GameButton ExchangeQuantityBtn => new(Paths.AnniversaryShopLoc.ExchangeLoc.ChangeQuantityBtn);

    private static GameText ExchangeQuantityTxt => new(Paths.AnniversaryShopLoc.ExchangeLoc.ChangeQuantityTxt);

    private static readonly string[] QuantityCandidatesDescending = { "10", "5" };

    /// <summary>Same bulk-quantity-multiplier pattern as DecoratedHeroesShop/ChaosRift.</summary>
    public static IEnumerator TrySetBestQuantity()
    {
        var startText = ExchangeQuantityTxt.GetParsedText();
        Logger.Debug($"[AnniversaryShop] TrySetBestQuantity: current quantity text = '{startText}'.");

        if (QuantityCandidatesDescending.Any(c => startText.Contains(c)))
            yield break;

        var original = startText;
        var found = false;

        for (var i = 0; i < 6; i++)
        {
            yield return ExchangeQuantityBtn.Click();

            if (QuantityCandidatesDescending.Any(c => ExchangeQuantityTxt.GetParsedText().Contains(c)))
            {
                found = true;
                break;
            }

            if (ExchangeQuantityTxt.GetParsedText() == original) break;
        }

        Logger.Debug($"[AnniversaryShop] TrySetBestQuantity: found={found}, ended at '{ExchangeQuantityTxt.GetParsedText()}'.");

        if (found) yield break;

        for (var i = 0; i < 6 && ExchangeQuantityTxt.GetParsedText() != original; i++)
            yield return ExchangeQuantityBtn.Click();
    }

    /// <summary>
    ///     Buys itemName repeatedly until unaffordable/capped. Same CurrencyMissingPopup check as
    ///     DecoratedHeroesShop.BuyItem - live-confirmed there that this shop family's buy buttons stay
    ///     clickable regardless of real balance, so IsClickable() alone can't be trusted to stop the
    ///     loop at the real affordability limit.
    /// </summary>
    public static IEnumerator BuyItem(string itemName, int maxBuys = 100)
    {
        var items = new GameElement(Paths.AnniversaryShopLoc.ExchangeLoc.ItemsRoot).GetChildren().ToList();
        var names = string.Join(", ", items.Select(i =>
            new GameText(Paths.AnniversaryShopLoc.ExchangeLoc.ItemNameTxt, i).GetParsedText()));
        Logger.Debug($"[AnniversaryShop] BuyItem('{itemName}'): {items.Count} item(s) scanned: {names}");

        foreach (var item in items)
        {
            var name = new GameText(Paths.AnniversaryShopLoc.ExchangeLoc.ItemNameTxt, item).GetParsedText();
            if (!name.Contains(itemName, StringComparison.OrdinalIgnoreCase)) continue;

            var buyBtn = new GameButton(Paths.AnniversaryShopLoc.ExchangeLoc.ItemBuyBtn, item);
            var bought = 0;
            for (var i = 0; i < maxBuys && buyBtn.IsClickable(); i++)
            {
                yield return buyBtn.Click();

                if (CurrencyMissingPopup.IsShowing)
                {
                    Logger.Debug($"[AnniversaryShop] BuyItem: currency ran out after {bought} real purchase(s) - this click didn't go through.");
                    yield return CurrencyMissingPopup.Close;
                    break;
                }

                bought++;
            }

            Logger.Debug($"[AnniversaryShop] BuyItem: matched '{name}', bought {bought}x.");
            yield break;
        }

        Logger.Debug($"[AnniversaryShop] BuyItem: no item matching '{itemName}' found.");
    }
}
