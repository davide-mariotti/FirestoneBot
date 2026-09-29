using System.Collections;
using Firebot.GameModel.Features.Town;
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

    protected override IEnumerator Work(int missing)
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenTavern;

        yield return Tavern.TrySetPlayQuantityTo(1);
        Debug($"[INFO] Gamer: {missing} draw(s) missing, {Tavern.GameTokenCount} token(s), single draw={Tavern.IsSingleDraw}.");

        if (Tavern.IsSingleDraw)
            for (var i = 0; i < missing && Tavern.GameTokenCount > 0 && Tavern.PlayBtn.IsClickable(); i++)
                yield return Tavern.PlayRound();

        yield return Tavern.Close;
        yield return TownScreen.Close;
    }
}
