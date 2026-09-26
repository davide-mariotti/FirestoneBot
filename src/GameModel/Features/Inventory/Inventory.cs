using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

namespace Firebot.GameModel.Features.Inventory;

public static class Inventory
{
    // See UiVariantButton - this bottom-bar HUD variant switches dynamically within a session
    // (confirmed live, 2026-09-17), not just once per session like the notification rail. Tries
    // every known location instead of assuming one is "the" active one.
    public static IEnumerator Open => UiVariantButton.Click(
        new GameButton(Paths.BattleLoc.BottomRightSideUINewLoc.InventoryBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIMobileLoc.InventoryBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIDesktopLoc.InventoryBtn));

    public static IEnumerator OpenChestsTab => new GameButton(Paths.InventoryLoc.ChestsTabBtn).Click();

    public static IEnumerator OpenItemsTab => new GameButton(Paths.InventoryLoc.ItemsTabBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.InventoryLoc.CloseBtn).Click();

    public static GameElement Content => new(Paths.InventoryLoc.ContentRoot);

    /// <summary>
    ///     Clicks every item slot (in whichever tab is currently open) whose name looks like one of
    ///     the "instant gold" conversion items (Pouch/Bucket/Crate/Pile of Gold - these convert to
    ///     meteorites when used, per the user). No confirmed exact slot names found via UnityPy (the
    ///     only "gold" GameObjects found were a VFX holder, not the clickable slot itself), so this
    ///     matches generically by name instead of hardcoding possibly-wrong names - safe no-op on
    ///     anything that isn't actually clickable.
    /// </summary>
    public static IEnumerator UseAllGoldItems()
    {
        // Live-confirmed, 2026-09-18: consuming a gold slot can compact the grid (later items shift
        // into the now-empty position) - clicking a cached path name repeatedly ended up hitting
        // whatever unrelated item slid in after the gold ran out (the user saw totems get consumed
        // this way). Re-reading the real name at each position fresh every iteration instead of
        // trusting a name captured once up front.
        while (true)
        {
            var goldSlot = Content.GetChildren()
                .FirstOrDefault(c => !string.IsNullOrEmpty(c.Name) && c.Name.ToLowerInvariant().Contains("gold"));
            if (goldSlot == null) yield break;

            var slot = new GameButton("/" + goldSlot.Name, Content);
            if (!slot.IsClickable()) yield break;
            yield return slot.ClickSimulated();
        }
    }
}

/// <summary>
///     One chest opening flow: click a chest slot (e.g. "commonChestbox") in Inventory.Content,
///     repeatedly pick the biggest available batch (x10 then x1) via ChestOpenPreview/ChestOpening
///     until the desired remaining quantity is reached, then close back to the Inventory grid.
/// </summary>
public static class ChestOpening
{
    // The chest-opening reveal (rarer chests especially) plays a variable-length animation before
    // the next screen's buttons become interactable - confirmed live, 2026-09-18: a fixed post-click
    // delay (InteractionDelay) outran it, so the next click landed before the game was ready. Polling
    // for the actual next button instead of guessing a duration - same pattern as ArenaOfKingsTask's
    // battle-result wait.
    private static readonly WaitForSeconds ChestTransitionPollWait = new(0.3f);
    private const int MaxChestTransitionPolls = 25; // ~7.5s ceiling

    /// <summary>
    ///     Opens chests of the given slot (relative to Inventory.Content, e.g. "/commonChestbox")
    ///     down to (not below) targetRemaining. Safe no-op if the slot doesn't exist or is already
    ///     at/below target - every click here is gated by IsClickable() first. onOpened, if given, is
    ///     invoked once at the end with the actual number opened (per the user, 2026-09-23, so
    ///     callers can track daily quest progress without re-reading screen state themselves).
    /// </summary>
    public static IEnumerator OpenDownTo(string slotPath, int targetRemaining, Action<int> onOpened = null)
    {
        var slot = new GameButton(slotPath, Inventory.Content);
        var slotClickable = slot.IsClickable();

        var quantityTxt = new GameText(slotPath + "/quantity", Inventory.Content);
        if (!slotClickable) yield break;

        var remainingToOpen = quantityTxt.GetParsedInt() - targetRemaining;
        if (remainingToOpen <= 0) yield break;

        var totalToOpen = remainingToOpen;

        // Live-confirmed, 2026-09-26 via Watchdog.DumpActiveScreens() right after the click: these
        // slots are pooled ScrollView cells whose Button.onClick has zero listeners wired, exactly the
        // case ClickSimulated's own doc comment describes - plain Click() is a complete no-op here
        // (confirmed: active screen stayed "menus/Inventory" indefinitely, no popup ever appeared).
        yield return slot.ClickSimulated(); // opens ChestOpenPreview

        var onPreview = true;
        var openPreviewAttempts = 0;
        const int MaxOpenPreviewAttempts = 3;

        while (remainingToOpen > 0)
        {
            var openX10 = new GameButton(onPreview
                ? Paths.ChestOpenPreviewLoc.OpenX10Btn
                : Paths.ChestOpeningLoc.OpenX10Btn);
            var openX1 = new GameButton(onPreview
                ? Paths.ChestOpenPreviewLoc.OpenX1Btn
                : Paths.ChestOpeningLoc.OpenX1Btn);

            var pollsLeft = MaxChestTransitionPolls;
            while (pollsLeft > 0 && !openX10.IsClickable() && !openX1.IsClickable())
            {
                yield return ChestTransitionPollWait;
                pollsLeft--;
            }

            if (remainingToOpen >= 10 && openX10.IsClickable())
            {
                Firebot.Core.Logger.Debug($"[ChestOpening] '{slotPath}': clicking openX10 (onPreview={onPreview}).");
                yield return openX10.Click();
                remainingToOpen -= 10;
            }
            else if (openX1.IsClickable())
            {
                Firebot.Core.Logger.Debug($"[ChestOpening] '{slotPath}': clicking openX1 (onPreview={onPreview}).");
                yield return openX1.Click();
                remainingToOpen -= 1;
            }
            else if (onPreview && ++openPreviewAttempts < MaxOpenPreviewAttempts)
            {
                // Live-confirmed, 2026-09-26: the very first slot processed in a run intermittently
                // fails to open ChestOpenPreview at all on the first click (its whole popup path
                // never resolves, not just its buttons) - every later slot in the same run opens on
                // the first try, so this isn't the click method, just the first click after the
                // Chests tab settles occasionally not landing. Retry the slot click itself instead
                // of concluding "no chests" on real, present ones.
                Firebot.Core.Logger.Debug($"[ChestOpening] '{slotPath}': neither button available after poll " +
                                           $"(attempt {openPreviewAttempts}/{MaxOpenPreviewAttempts}) - retrying slot click.");
                yield return slot.ClickSimulated();
                continue;
            }
            else
            {
                Firebot.Core.Logger.Debug($"[ChestOpening] '{slotPath}': giving up, neither button ever became " +
                                           $"available (onPreview={onPreview}, openPreviewAttempts={openPreviewAttempts}).");
                break; // neither button ever became available - out of chests, or the flow ended on its own
            }

            onPreview = false; // every subsequent click happens on the ChestOpening results screen
        }

        yield return new GameButton(Paths.ChestOpeningLoc.CloseBtn).Click();
        yield return new GameButton(Paths.ChestOpenPreviewLoc.CloseBtn).Click(); // safe no-op if already closed

        var actuallyOpened = totalToOpen - remainingToOpen;
        Firebot.Core.Logger.Debug($"[ChestOpening] '{slotPath}': done, opened {actuallyOpened}/{totalToOpen}.");
        onOpened?.Invoke(actuallyOpened);
    }

    /// <summary>Opens every owned chest of the given slot (down to 0).</summary>
    public static IEnumerator OpenAll(string slotPath, Action<int> onOpened = null) =>
        OpenDownTo(slotPath, 0, onOpened);
}
