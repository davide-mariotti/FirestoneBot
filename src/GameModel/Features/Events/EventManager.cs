using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Events;

/// <summary>
///     The Events hub (Battle -&gt; Events). Deliberately generic: this whole class is meant to be
///     reused unchanged by every future event task, only the per-event shop screen (e.g.
///     DecoratedHeroesShop) is event-specific. See Paths.EventManagerLoc for the (not yet
///     live-confirmed) root mount.
/// </summary>
public static class EventManager
{
    public static bool IsVisible => new GameElement(Paths.EventManagerLoc.Root).IsVisible();

    public static IEnumerator Open =>
        new GameButton(Paths.BattleLoc.BottomSideUIMobileLoc.EventsBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.EventManagerLoc.CloseBtn).Click();

    private static IEnumerable<GameElement> AllCards =>
        new GameElement(Paths.EventManagerLoc.ActiveEventsRoot).GetChildren()
            .Concat(new GameElement(Paths.EventManagerLoc.UpcomingEventsRoot).GetChildren());

    /// <summary>
    ///     Scans both the active and upcoming event lists for a card whose title text matches
    ///     eventName (case-insensitive, partial match - same style as the priority-name matching in
    ///     FirestoneResearchTask), and clicks it if found and clickable. Safe no-op otherwise (e.g.
    ///     the event isn't currently running, or is upcoming/locked) - the caller's own IsVisible
    ///     check on the resulting shop screen is what actually confirms success.
    /// </summary>
    public static IEnumerator OpenEvent(string eventName)
    {
        foreach (var card in AllCards)
        {
            var title = new GameText(Paths.EventManagerLoc.CardTitleTxt, card).GetParsedText();
            if (!title.Contains(eventName, StringComparison.OrdinalIgnoreCase)) continue;

            var button = new GameButton(parent: card);
            if (button.IsClickable()) yield return button.Click();
            yield break;
        }
    }
}
