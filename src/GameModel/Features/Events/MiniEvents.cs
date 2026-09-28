using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Events;

/// <summary>Mass Production's screen. Only the challenges tab is used - its default "offers" tab is paid.</summary>
public static class MiniEvents
{
    public static bool IsVisible => new GameElement(Paths.MiniEventsLoc.Root).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.MiniEventsLoc.CloseBtn).Click();

    public static IEnumerator OpenChallengesTab => new GameButton(Paths.MiniEventsLoc.ChallengesTabBtn).Click();

    /// <summary>Claims every unlocked day. A locked day's claim button isn't visible, so IsClickable skips it.</summary>
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
