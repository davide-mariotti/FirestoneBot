using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town;

public static class Tavern
{
    // The card reveal takes longer for a bigger batch.
    private const int MaxAnimationPolls = 25;
    private const float AnimationPollSeconds = 0.3f;

    public static IEnumerator OpenMarket => new GameButton(Paths.MenusLoc.TavernLoc.OpenMarketBtn).Click();

    public static GameButton PlayBtn => new(Paths.MenusLoc.TavernLoc.PlayBtn);

    private static GameButton FirstCardBtn => new(Paths.MenusLoc.TavernLoc.FirstCardBtn);

    /// <summary>
    ///     One round at the current quantity. Play alone only lays out face-down cards - no tokens
    ///     are spent and the quest doesn't count it until a card is picked. Any card will do.
    /// </summary>
    public static IEnumerator PlayRound()
    {
        yield return PlayBtn.Click();
        yield return Poll.Until(() => FirstCardBtn.IsClickable(), MaxAnimationPolls, AnimationPollSeconds);

        yield return FirstCardBtn.Click();
        yield return Poll.Until(() => PlayBtn.IsClickable(), MaxAnimationPolls, AnimationPollSeconds);
    }

    public static int GameTokenCount => new GameText(Paths.MenusLoc.TavernLoc.GameTokenCountTxt).GetParsedInt();

    private static GameText PlayQuantityTxt => new(Paths.MenusLoc.TavernLoc.QuantityTxt);

    // "x10" -> 10; -1 when unreadable.
    private static int ParseQuantity(string text) =>
        int.TryParse(text.TrimStart('x', 'X').Trim(), out var n) ? n : -1;

    public static bool IsPlayQuantitySetTo(int quantity) => ParseQuantity(PlayQuantityTxt.GetParsedText()) == quantity;

    /// <summary>
    ///     Tries to select exactly this play quantity, so the daily 10 draws take one click. Check
    ///     IsPlayQuantitySetTo afterwards: the option may not exist.
    /// </summary>
    public static IEnumerator TrySetPlayQuantityTo(int quantity) => QuantityToggle.CycleUntil(
        new GameButton(Paths.MenusLoc.TavernLoc.ChangeQuantityBtn), PlayQuantityTxt,
        text => ParseQuantity(text) == quantity);

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.TavernLoc.CloseBtn).Click();
}
