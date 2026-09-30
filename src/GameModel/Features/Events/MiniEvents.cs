using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Il2CppTMPro;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Events;

/// <summary>
///     Every mini-event's screen (Mass Production, Sigils of Prophecy, ...). Only the challenges tab is
///     used - its default "offers" tab is paid.
/// </summary>
public static class MiniEvents
{
    public static bool IsVisible => new GameElement(Paths.MiniEventsLoc.Root).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.MiniEventsLoc.CloseBtn).Click();

    public static IEnumerator OpenChallengesTab => new GameButton(Paths.MiniEventsLoc.ChallengesTabBtn).Click();

    /// <summary>
    ///     Text and progress ("1/2") of every unlocked day not claimed yet. Read from the Transforms, so a
    ///     locked day - a normal state - isn't logged as a failure.
    /// </summary>
    public static List<(string Text, string Progress)> Challenges()
    {
        var challenges = new List<(string, string)>();
        var grid = GameElement.FindTransform(Paths.MiniEventsLoc.ChallengesGridRoot);
        if (grid == null) return challenges;

        for (var i = 0; i < grid.childCount; i++)
        {
            var card = grid.GetChild(i);
            if (!IsActive(card, Paths.MiniEventsLoc.ChallengeUnlocked) ||
                IsActive(card, Paths.MiniEventsLoc.ChallengeClaimedTxt)) continue;

            challenges.Add((Text(card, Paths.MiniEventsLoc.ChallengeTitleTxt),
                Text(card, Paths.MiniEventsLoc.ChallengeProgressTxt)));
        }

        return challenges;
    }

    private static bool IsActive(Transform card, string path) => card.Find(path)?.gameObject.activeInHierarchy == true;

    private static string Text(Transform card, string path) => card.Find(path)?.GetComponent<TMP_Text>()?.text ?? "";

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
