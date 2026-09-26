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
///     (earned by staying online a certain amount of time that day), and buys "Meteorite" from the
///     Exchange (the account's target item, changed 2026-09-26 from "Rare chest" - Meteorite is more
///     valuable). Avatars/Skins/the real-money Shop tab deliberately untouched, same convention
///     as DecoratedHeroesEventTask's own excluded tabs.
/// </summary>
public class NewPlayerEventTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Events;

    // Per the user (2026-09-26): buy Meteorite instead of the first chest-type item - more valuable
    // to the account. Exchange list order: Epic chest, Golden chest, Exotic coin, Pickaxe, Strange
    // dust, Honor, Beer, Meteorite (last).
    private const string TargetExchangeItem = "Meteorite";

    // Per the user (2026-09-26): recheck hourly instead of every 4h, so the daily check-in and any
    // newly-unlocked activity milestone get claimed promptly instead of piling up.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(1);

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
