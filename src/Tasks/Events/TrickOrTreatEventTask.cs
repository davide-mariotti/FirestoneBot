using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Features.Events;

namespace Firebot.Tasks.Events;

/// <summary>
///     Halloween: the pumpkins Maxwell drops (3,000 a day, picked up by the game itself) go to the
///     witch's hut exchange, hourly, before the event ends and they're lost.
/// </summary>
public class TrickOrTreatEventTask : EventTask
{
    protected override string[] EventNames => new[] { "Trick or treat" };

    protected override bool IsScreenVisible => TrickOrTreatShop.IsVisible;

    // No challenges: exchange only.
    protected override IEnumerator RunEvent(List<(string Text, string Progress)> challenges)
    {
        yield return TrickOrTreatShop.OpenExchangeTab;
        yield return TrickOrTreatShop.Exchange.BuyPriorityItems();

        yield return TrickOrTreatShop.Close;
    }
}
