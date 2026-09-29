using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = Firebot.Core.Logger;
using PirateShip = Firebot.GameModel.Features.Town.PirateShip.PirateShip;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Claims Pirate's Prize's free track - never the paid one. The ~20 tiers all share one name, so
///     a path can't address a specific tier (it always resolves to the first): this walks the raw
///     Transforms by index and clicks each tier's own button.
/// </summary>
public class PiratesPrizeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 10;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.PiratesPrize;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);
    private static readonly WaitForSeconds ClickSettleWait = new(0.5f);

    public override IEnumerator Execute()
    {
        yield return Notifications.PiratesPrize;

        // No tab click: Pirate's Prize is already open, and clicking its tab resets the list's scroll.
        yield return TownScreen.Open;
        yield return TownScreen.OpenPirateShip;

        // The game unloads the Pirate Ship menu when unused, and rebuilding it outlasts the click delay.
        yield return Poll.Until(() =>
            GameElement.FindTransform(Paths.PirateShipLoc.PiratesPrizeLoc.TierListRoot)?.childCount > 0);

        var tierListRoot = GameElement.FindTransform(Paths.PirateShipLoc.PiratesPrizeLoc.TierListRoot);

        if (tierListRoot == null)
        {
            Logger.Debug("[PiratesPrizeTask] Tier list root not found - skipping claim scan.");
        }
        else
        {
            for (var i = 0; i < tierListRoot.childCount; i++)
            {
                var tier = tierListRoot.GetChild(i);
                if (!tier.name.StartsWith("ppTierInteraction")) continue;

                if (TryClaimTier(tier)) yield return ClickSettleWait;
            }
        }

        yield return PirateShip.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    // These claim buttons have no onClick listeners, so this sends the simulated pointer click.
    private static bool TryClaimTier(Transform tier)
    {
        var claimButton = tier.Find(Paths.PirateShipLoc.PiratesPrizeLoc.TierFreeClaimBtn);
        if (claimButton == null || !claimButton.gameObject.activeInHierarchy) return false;

        if (!claimButton.TryGetComponent<Button>(out var button) || !button.enabled || !button.interactable)
            return false;

        if (EventSystem.current == null)
        {
            Logger.Debug("[PiratesPrizeTask] [FAILED] Claim skipped: no EventSystem in scene.");
            return false;
        }

        try
        {
            GameButton.DispatchPointerClick(claimButton.gameObject);
            return true;
        }
        catch (Exception e)
        {
            Logger.Debug($"[PiratesPrizeTask] [FAILED] Claim click threw: {e.Message}.");
            return false;
        }
    }
}
