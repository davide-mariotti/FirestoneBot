using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Events;

/// <summary>
///     "Mass Production"'s real screen - see Paths.MiniEventsLoc's own doc comment for why it's named
///     "MiniEvents" internally. Only the "challenges" tab is wired here, per the user - the default
///     "offers" tab (real-money packs) is deliberately never touched.
/// </summary>
public static class MiniEvents
{
    public static bool IsVisible => new GameElement(Paths.MiniEventsLoc.Root).IsVisible();

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

    public static IEnumerator Close => new GameButton(Paths.MiniEventsLoc.CloseBtn).Click();

    public static IEnumerator OpenChallengesTab => new GameButton(Paths.MiniEventsLoc.ChallengesTabBtn).Click();

    /// <summary>
    ///     Claims every currently-unlocked day's challenge - same "whole grid, click whatever's
    ///     clickable" idiom as DecoratedHeroesShop.ClaimAllChallenges. A not-yet-unlocked day's claim
    ///     button lives under an inactive "unlocked" container, so IsClickable() (which checks
    ///     visibility first) already skips it without needing a separate locked check.
    /// </summary>
    public static IEnumerator ClaimAllChallenges()
    {
        var cards = new GameElement(Paths.MiniEventsLoc.ChallengesGridRoot).GetChildren().ToList();
        Logger.Debug($"[MiniEvents] ClaimAllChallenges: {cards.Count} card(s) under ChallengesGridRoot.");

        var claimed = 0;
        foreach (var card in cards)
        {
            var claimBtn = new GameButton(Paths.MiniEventsLoc.ChallengeClaimBtn, card);
            if (!claimBtn.IsClickable()) continue;

            yield return claimBtn.Click();
            claimed++;
        }

        Logger.Debug($"[MiniEvents] ClaimAllChallenges: claimed {claimed}/{cards.Count}.");
    }
}
