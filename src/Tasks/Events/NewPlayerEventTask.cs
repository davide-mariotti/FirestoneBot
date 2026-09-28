using System.Collections;
using Firebot.GameModel.Features.Events;

namespace Firebot.Tasks.Events;

/// <summary>
///     New Player Event (its screen is the game's AnniversaryShop): claims the daily check-in and the
///     activity milestones, then spends the event currency on the exchange. Avatars, Skins and the
///     paid Shop tab are never touched.
/// </summary>
public class NewPlayerEventTask : EventTask
{
    protected override string EventName => "New Player Event";

    protected override bool IsScreenVisible => AnniversaryShop.IsVisible;

    protected override IEnumerator RunEvent()
    {
        yield return AnniversaryShop.OpenDailiesTab;
        yield return AnniversaryShop.ClaimDailyCheckIn();

        yield return AnniversaryShop.OpenActivityTab;
        yield return AnniversaryShop.ClaimActivityMilestones();

        yield return AnniversaryShop.OpenExchangeTab;
        yield return AnniversaryShop.Exchange.BuyPriorityItems();

        yield return AnniversaryShop.Close;
    }
}
