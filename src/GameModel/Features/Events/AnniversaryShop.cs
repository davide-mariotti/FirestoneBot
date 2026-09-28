using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;
using UnityEngine.UI;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Events;

/// <summary>New Player Event's shop. Only Dailies, Activity and Exchange are used - never Avatars, Skins or Shop.</summary>
public static class AnniversaryShop
{
    public static bool IsVisible => new GameElement(Paths.AnniversaryShopLoc.Root).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.AnniversaryShopLoc.CloseBtn).Click();

    public static IEnumerator OpenDailiesTab => new GameButton(Paths.AnniversaryShopLoc.DailiesTabBtn).Click();

    public static IEnumerator OpenActivityTab => new GameButton(Paths.AnniversaryShopLoc.ActivityTabBtn).Click();

    public static IEnumerator OpenExchangeTab => new GameButton(Paths.AnniversaryShopLoc.ExchangeTabBtn).Click();

    public static readonly ExchangeTab Exchange = new("AnniversaryShop",
        Paths.AnniversaryShopLoc.ExchangeLoc.ChangeQuantityBtn, Paths.AnniversaryShopLoc.ExchangeLoc.ChangeQuantityTxt,
        Paths.AnniversaryShopLoc.ExchangeLoc.ItemsRoot, Paths.AnniversaryShopLoc.ExchangeLoc.ItemNameTxt,
        Paths.AnniversaryShopLoc.ExchangeLoc.ItemBuyBtn);

    public static IEnumerator ClaimDailyCheckIn()
    {
        var btn = new GameButton(Paths.AnniversaryShopLoc.DailiesLoc.CheckInBtn);
        var clickable = btn.IsClickable();
        Logger.Debug($"[AnniversaryShop] ClaimDailyCheckIn: clickable={clickable}.");
        if (clickable) yield return btn.Click();
    }

    /// <summary>
    ///     Claims every milestone that's ready. Past the 4th, the milestones are clones that all share
    ///     one name ("milestone (3)(Clone)"), so a path would always resolve to the first of them -
    ///     this walks the raw Transforms by index instead.
    /// </summary>
    public static IEnumerator ClaimActivityMilestones()
    {
        var milestonesRoot = GameElement.FindTransform(Paths.AnniversaryShopLoc.ActivityLoc.MilestonesRoot);

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
    }

    // A plain onClick.Invoke() works on these, unlike Pirate's Prize's claim buttons.
    private static bool TryClaimMilestone(Transform milestone)
    {
        var claimButton = milestone.Find(Paths.AnniversaryShopLoc.ActivityLoc.MilestoneClaimBtn);
        if (claimButton == null || !claimButton.gameObject.activeInHierarchy) return false;

        if (!claimButton.TryGetComponent<Button>(out var button) || !button.enabled || !button.interactable)
            return false;

        button.onClick.Invoke();
        return true;
    }
}
