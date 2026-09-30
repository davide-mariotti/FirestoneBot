using System;
using System.Collections;
using Firebot.GameModel.Features.Town;
using Logger = Firebot.Core.Logger;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     The daily "Gamer" quest: 10 Tavern card draws, one game token each at x1 (BeerExchangeTask
///     turns beer into tokens). Plays as many draws as the quest is missing, while there are tokens -
///     nothing else spends them, so there's no reserve to keep.
/// </summary>
public class GamerQuestTask : DailyQuestTask
{
    protected override int MinimumCharacterLevel => 15;

    protected override string QuestName => "Gamer";

    protected override IEnumerator Work(int missing) => Play(missing);

    /// <summary>
    ///     Up to `draws` single draws, while there are tokens. Also an event challenge's
    ///     (EventChallengeActions). onPlayed gets how many were played.
    /// </summary>
    public static IEnumerator Play(int draws, Action<int> onPlayed = null)
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenTavern;

        yield return Tavern.TrySetPlayQuantityTo(1);
        Logger.Debug($"[INFO] Tavern: {draws} draw(s) to play, {Tavern.GameTokenCount} token(s), single draw={Tavern.IsSingleDraw}.");

        var played = 0;
        if (Tavern.IsSingleDraw)
            for (; played < draws && Tavern.GameTokenCount > 0 && Tavern.PlayBtn.IsClickable(); played++)
                yield return Tavern.PlayRound();

        onPlayed?.Invoke(played);

        yield return Tavern.Close;
        yield return TownScreen.Close;
    }
}
