using System;
using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Inventory;

public static class Inventory
{
    public static IEnumerator Open => UiVariantButton.Click(
        new GameButton(Paths.BattleLoc.BottomRightSideUINewLoc.InventoryBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIMobileLoc.InventoryBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIDesktopLoc.InventoryBtn));

    public static IEnumerator OpenChestsTab => new GameButton(Paths.InventoryLoc.ChestsTabBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.InventoryLoc.CloseBtn).Click();

    public static GameElement Content => new(Paths.InventoryLoc.ContentRoot);
}

/// <summary>Opens chests of one slot: preview popup, then x10/x1 batches on the results screen.</summary>
public static class ChestOpening
{
    // The reveal animation (longer for rarer chests) outlasts the standard interaction delay.
    private const int MaxChestTransitionPolls = 25;
    private const float ChestTransitionPollSeconds = 0.3f;

    /// <summary>
    ///     Opens chests of a slot (relative to Inventory.Content, e.g. "/Common") down to, never
    ///     below, targetRemaining, and at most maxToOpen of them. onOpened gets the number actually
    ///     opened, so a daily-quest caller can count progress without re-reading the screen.
    /// </summary>
    public static IEnumerator OpenDownTo(string slotPath, int targetRemaining, Action<int> onOpened = null,
        int maxToOpen = int.MaxValue)
    {
        var slot = new GameButton(slotPath, Inventory.Content);
        var slotClickable = slot.IsClickable();

        var quantityTxt = new GameText(slotPath + "/quantity", Inventory.Content);
        if (!slotClickable) yield break;

        var remainingToOpen = Math.Min(quantityTxt.GetParsedInt() - targetRemaining, maxToOpen);
        if (remainingToOpen <= 0) yield break;

        var totalToOpen = remainingToOpen;

        // Chest slots are pooled list cells whose Button has no onClick listeners.
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

            yield return Poll.Until(() => openX10.IsClickable() || openX1.IsClickable(),
                MaxChestTransitionPolls, ChestTransitionPollSeconds);

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
                // The first slot of a run sometimes ignores its click entirely - retry it rather than
                // conclude there are no chests.
                Firebot.Core.Logger.Debug($"[ChestOpening] '{slotPath}': neither button available after poll " +
                                           $"(attempt {openPreviewAttempts}/{MaxOpenPreviewAttempts}) - retrying slot click.");
                yield return slot.ClickSimulated();
                continue;
            }
            else
            {
                Firebot.Core.Logger.Debug($"[ChestOpening] '{slotPath}': giving up, neither button ever became " +
                                           $"available (onPreview={onPreview}, openPreviewAttempts={openPreviewAttempts}).");
                break; // out of chests, or the flow ended on its own
            }

            onPreview = false; // every later click happens on the ChestOpening results screen
        }

        yield return new GameButton(Paths.ChestOpeningLoc.CloseBtn).Click();
        yield return new GameButton(Paths.ChestOpenPreviewLoc.CloseBtn).Click(); // no-op if already closed

        var actuallyOpened = totalToOpen - remainingToOpen;
        Firebot.Core.Logger.Debug($"[ChestOpening] '{slotPath}': done, opened {actuallyOpened}/{totalToOpen}.");
        onOpened?.Invoke(actuallyOpened);
    }

    /// <summary>Opens every chest of the given slot.</summary>
    public static IEnumerator OpenAll(string slotPath, Action<int> onOpened = null) =>
        OpenDownTo(slotPath, 0, onOpened);
}
