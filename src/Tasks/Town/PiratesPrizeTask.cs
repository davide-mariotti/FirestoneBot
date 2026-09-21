using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
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
///     Pirate's Prize - claims the FREE track of a character-level-gated reward track inside the
///     Pirate Ship building (unlocks at level 10 per the wiki). Never touches the paid/premium
///     track (piratesPrizePaidInteraction/purchasePremiumButton, and the catch-up
///     previousTierInteraction/purchasePrice, and currentSection/paidRewardsButton/purchasePrice) -
///     per docs/screens/PirateShip.html and live-confirmed button naming, all of those spend real
///     currency to individually unlock a reward, unlike Battle Pass's Golden track (bought once,
///     then every already-earned tier is a free claim). "Mercenaries", "Captain's Deal" and "Skins"
///     (the ship's other tabs) are all purchase/cosmetic flows, out of scope.
///     Live-confirmed, 2026-09-18 (3 rounds of live diagnostics): the tier list
///     (bg/submenus/piratesPrize/Scroll View/Viewport/content/rewardsTierSection) is NOT actually
///     pooled/virtualized - all ~20 "ppTierInteraction(Clone)" items exist in the hierarchy
///     simultaneously regardless of scroll position (a verbose per-step scan proved tierCount stayed
///     constant across the whole scroll range) - the ScrollRect just visually pans over a fixed set.
///     The REAL bug: every one of those ~20 tier siblings shares the EXACT SAME name
///     ("ppTierInteraction(Clone)", no per-instance index unlike every other pooled list in this
///     codebase), and GameElement/GameButton always re-resolve by STRING PATH on every access (see
///     GameElement's own "Always resolve from Path; do not cache transforms" comment) - so
///     Transform.Find("ppTierInteraction(Clone)") always matched the SAME (first) sibling no matter
///     which conceptual tier index the loop thought it was checking, making every iteration
///     re-inspect one single tier repeatedly instead of 20 different ones. This task therefore
///     bypasses GameElement/GameButton entirely for the tier scan, walking raw Transform children
///     and resolving each tier's own claim button directly from that already-known Transform - see
///     TryClaimTier.
///     This whole feature is brand new, requested by the user, never automated before.
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
        // Fast path - opportunistic only (see Battle.cs), safe no-op if not up.
        yield return Notifications.PiratesPrize;

        // Guaranteed path regardless of the notification - same reasoning as every other task.
        // Deliberately does NOT click the tab button - see PirateShipLoc.PiratesPrizeTabBtn.
        yield return TownScreen.Open;
        yield return TownScreen.OpenPirateShip;

        var tierListRoot = ResolveRawTransform(Paths.PirateShipLoc.PiratesPrizeLoc.TierListRoot);

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

    /// <summary>
    ///     Resolves a shared Paths.*Loc path string ("rootObjectName/relative/path") straight to its
    ///     Transform, for the one spot in this task that needs a real Transform reference instead of
    ///     GameElement's string-re-resolving wrapper - see the class doc comment for why.
    /// </summary>
    private static Transform ResolveRawTransform(string path)
    {
        var slashIndex = path.IndexOf('/');
        var rootObject = GameObject.Find(slashIndex == -1 ? path : path[..slashIndex]);
        if (rootObject == null) return null;

        return slashIndex == -1 ? rootObject.transform : rootObject.transform.Find(path[(slashIndex + 1)..]);
    }

    /// <summary>
    ///     Resolves and clicks THIS specific tier's free-claim button via its own already-known
    ///     Transform (never by re-resolving a name that ~20 siblings share) - see the class doc
    ///     comment for why. Same ExecuteEvents-based simulated click as GameButton.ClickSimulated
    ///     (confirmed live, 2026-09-18: the plain Button.onClick has no listeners wired here either,
    ///     same pooled-list quirk already fixed for chest slots and gold items).
    /// </summary>
    private static bool TryClaimTier(Transform tier)
    {
        var freeInteraction = tier.Find("piratesPrizeFreeInteraction");
        var claimButton = freeInteraction != null ? freeInteraction.Find("purchaseFreeButton") : null;
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
            var gameObject = claimButton.gameObject;
            var pointerData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                pointerPress = gameObject
            };

            ExecuteEvents.Execute(gameObject, pointerData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(gameObject, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(gameObject, pointerData, ExecuteEvents.pointerClickHandler);
            return true;
        }
        catch (Exception e)
        {
            Logger.Debug($"[PiratesPrizeTask] [FAILED] Claim click threw: {e.Message}.");
            return false;
        }
    }
}
