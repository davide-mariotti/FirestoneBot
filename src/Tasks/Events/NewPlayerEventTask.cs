using System;
using System.Collections;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Events;

namespace Firebot.Tasks.Events;

/// <summary>
///     "New Player Event" - the real in-game screen is a recycled "AnniversaryShop" prefab/script
///     (see AnniversaryShop's own doc comment for how this was discovered via live diagnostics,
///     2026-09-24, Steam-10). Per the user: claims the daily check-in, claims activity milestones
///     (earned by staying online a certain amount of time that day), and buys "Rare chest" from the
///     Exchange (the account's target item - the user described it by its position, "first" in the
///     Exchange list). Avatars/Skins/the real-money Shop tab deliberately untouched, same convention
///     as DecoratedHeroesEventTask's own excluded tabs.
/// </summary>
public class NewPlayerEventTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Events;

    private const string TargetExchangeItem = "Rare chest";

    // Recurring check, no fixed daily reset like a quest - recheck a few times a day so the daily
    // check-in and any newly-unlocked activity milestone get claimed promptly, same cadence reasoning
    // as DecoratedHeroesEventTask.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(4);

    public override IEnumerator Execute()
    {
        yield return EventManager.Open;
        Debug($"[INFO] EventManager hub visible after Open: {EventManager.IsVisible}");

        yield return EventManager.OpenEvent("New Player Event");
        yield return AnniversaryShop.WaitUntilOpen();
        Debug($"[INFO] AnniversaryShop visible after OpenEvent: {AnniversaryShop.IsVisible}");

        if (AnniversaryShop.IsVisible)
        {
            yield return AnniversaryShop.OpenDailiesTab;
            yield return AnniversaryShop.ClaimDailyCheckIn();

            yield return AnniversaryShop.OpenActivityTab;
            yield return AnniversaryShop.ClaimActivityMilestones();

            yield return AnniversaryShop.OpenExchangeTab;
            yield return AnniversaryShop.TrySetBestQuantity();
            yield return AnniversaryShop.BuyItem(TargetExchangeItem);

            yield return AnniversaryShop.Close;

            // Only schedule the long recheck on an actual completed pass - same reasoning as
            // DecoratedHeroesEventTask's own fix (2026-09-24): if the shop never opened, leaving
            // NextRunTime untouched lets BotManager's 2-minute idle floor retry soon instead of
            // going dark for 4 hours on every failure.
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else
        {
            Debug("[INFO] AnniversaryShop never opened - see EventManager/OpenEvent debug lines above for why.");
        }

        yield return EventManager.Close;
    }
}
