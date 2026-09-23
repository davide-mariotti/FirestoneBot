using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

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

    // See UiVariantButton - same three-location situation as BattlePass.Open's PathOfGloryBtn.
    // Live-confirmed, 2026-09-23 (4 rounds of live diagnostics after both bottom-bar variants and
    // several of their sub-containers turned out empty on a real session): the actual real button is
    // RightSideUILoc.EventsBtn (rightSideUI/menuButtons/eventsButton, alongside Town/Map/Guild/Store)
    // - missing from docs/path.firestone.html's static dump, likely added after that dump was taken.
    // Bottom-bar candidates kept as fallbacks in case a session genuinely uses one of them instead,
    // same defensive reasoning as PathOfGlory.
    public static IEnumerator Open => OpenRoutine();

    private static readonly WaitForSeconds OpenPollWait = new(0.5f);
    private const int MaxOpenPolls = 10;

    /// <summary>
    ///     Live-confirmed, 2026-09-23 (round 5 diagnostics): plain Click() (button.onClick.Invoke())
    ///     on eventsButton never throws and never fails IsClickable, but the hub genuinely never opens
    ///     even after polling up to 5s - same "real Button component, zero onClick listeners, driven
    ///     by something else instead" shape already documented on Store.Open's storeButton. Uses
    ///     ClickSimulated (real IPointerDown/Up/ClickHandler events) instead, same fix.
    /// </summary>
    private static IEnumerator OpenRoutine()
    {
        var candidates = new[]
        {
            new GameButton(Paths.BattleLoc.RightSideUILoc.EventsBtn),
            new GameButton(Paths.BattleLoc.BottomSideUIMobileLoc.EventsBtn),
            new GameButton(Paths.BattleLoc.BottomSideUIDesktopLoc.EventsBtn)
        };

        var target = candidates.FirstOrDefault(c => c.IsVisible()) ?? candidates[^1];
        yield return target.ClickSimulated();

        // Same reasoning as DecoratedHeroesShop.WaitUntilOpen: a hub transition can outlast the
        // standard interaction_delay - poll instead of checking immediately. Live-confirmed,
        // 2026-09-23: with ClickSimulated above, the hub opens and ActiveEventsRoot/UpcomingEventsRoot
        // (bg/verticalLayout/Scroll View/Viewport/Content/...) resolve exactly as originally guessed -
        // that internal structure was fine all along, just unreachable while the click itself no-opped.
        var pollsLeft = MaxOpenPolls;
        while (pollsLeft > 0 && !IsVisible)
        {
            yield return OpenPollWait;
            pollsLeft--;
        }
    }

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
