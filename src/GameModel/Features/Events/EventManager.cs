using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Events;

/// <summary>The Events hub (Battle -> Events). Generic: only each event's own shop screen is specific.</summary>
public static class EventManager
{
    public static bool IsVisible => new GameElement(Paths.EventManagerLoc.Root).IsVisible();

    public static IEnumerator Open => Poll.ClickUntil(VisibleEventsButton, () => IsVisible, "EventManager");

    // The Events button lives in a different HUD variant depending on the client.
    private static GameButton VisibleEventsButton()
    {
        var candidates = new[]
        {
            new GameButton(Paths.BattleLoc.RightSideUILoc.EventsBtn),
            new GameButton(Paths.BattleLoc.BottomSideUIMobileLoc.EventsBtn),
            new GameButton(Paths.BattleLoc.BottomSideUIDesktopLoc.EventsBtn)
        };

        return candidates.FirstOrDefault(c => c.IsVisible()) ?? candidates[^1];
    }

    public static IEnumerator Close => new GameButton(Paths.EventManagerLoc.CloseBtn).Click();

    private static IEnumerable<GameElement> AllCards =>
        new GameElement(Paths.EventManagerLoc.ActiveEventsRoot).GetChildren()
            .Concat(new GameElement(Paths.EventManagerLoc.UpcomingEventsRoot).GetChildren());

    /// <summary>
    ///     Opens the event whose card title contains eventName (case-insensitive), unless that card
    ///     is locked for this account. The caller confirms success through the shop's own IsVisible.
    ///     onCardFound reports whether an unlocked matching card exists, so a caller can tell "this
    ///     event isn't open to this account" (missing or locked) apart from "its shop failed to open
    ///     this time".
    /// </summary>
    public static IEnumerator OpenEvent(string eventName, Action<bool> onCardFound = null)
    {
        var cards = AllCards.ToList();
        Logger.Debug($"[EventManager] OpenEvent('{eventName}'): scanning {cards.Count} card(s): " +
                     string.Join(", ", cards.Select(c => $"{c.Name}='{new GameText(Paths.EventManagerLoc.CardTitleTxt, c).GetParsedText()}'")));

        foreach (var card in cards)
        {
            var title = new GameText(Paths.EventManagerLoc.CardTitleTxt, card).GetParsedText();
            if (!title.Contains(eventName, StringComparison.OrdinalIgnoreCase)) continue;

            // Locked counts as missing: retrying it every couple of minutes ran Sigils of Prophecy 65
            // times in a morning on Steam-15 and Steam-16 (30/09).
            if (new GameElement(Paths.EventManagerLoc.CardLockIndicator, card).IsVisible())
            {
                Logger.Debug($"[EventManager] Match '{title}' ({card.Name}) is locked for this account - skipping.");
                onCardFound?.Invoke(false);
                yield break;
            }

            onCardFound?.Invoke(true);

            // The hub closes once the event's own screen opens.
            var button = new GameButton(parent: card);
            yield return Poll.ClickUntil(() => button, () => !IsVisible, $"EventManager '{title}'");

            if (IsVisible)
                Logger.Debug($"[EventManager] '{title}' never opened despite not showing as locked - card structure:\n" +
                             Firebot.Core.Watchdog.DumpChildrenRecursive(card.FullPath, 4));

            yield break;
        }

        Logger.Debug($"[EventManager] No card matching '{eventName}' found.");
        onCardFound?.Invoke(false);
    }
}
