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

    // What to buy is now the shared EventExchangeConfig.PriorityItems order (Dragon blood ->
    // Meteorite -> Beer), 2026-09-28. This shop's own list is Epic chest, Golden chest, Exotic coin,
    // Pickaxe, Strange dust, Honor, Beer, Meteorite (last) - so Meteorite is what gets bought here,
    // with Beer as the leftover sink and Dragon blood a logged no-op (not sold in this shop). Same
    // net behaviour as the previous single "Meteorite" target, now with the leftovers spent instead
    // of left to expire with the event.
    // Per the user (2026-09-26): recheck hourly instead of every 4h, so the daily check-in and any
    // newly-unlocked activity milestone get claimed promptly instead of piling up.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(1);

    public override IEnumerator Execute()
    {
        yield return EventManager.Open;
        Debug($"[INFO] EventManager hub visible after Open: {EventManager.IsVisible}");

        var cardFound = false;
        yield return EventManager.OpenEvent("New Player Event", found => cardFound = found);
        yield return AnniversaryShop.WaitUntilOpen();
        Debug($"[INFO] AnniversaryShop visible after OpenEvent: {AnniversaryShop.IsVisible}, cardFound: {cardFound}");

        if (AnniversaryShop.IsVisible)
        {
            yield return AnniversaryShop.OpenDailiesTab;
            yield return AnniversaryShop.ClaimDailyCheckIn();

            yield return AnniversaryShop.OpenActivityTab;
            yield return AnniversaryShop.ClaimActivityMilestones();

            yield return AnniversaryShop.OpenExchangeTab;
            yield return AnniversaryShop.TrySetBestQuantity();
            foreach (var item in EventExchangeConfig.PriorityItems)
                yield return AnniversaryShop.BuyItem(item);

            yield return AnniversaryShop.Close;

            // Only schedule the long recheck on an actual completed pass - same reasoning as
            // DecoratedHeroesEventTask's own fix (2026-09-24): if the shop never opened, leaving
            // NextRunTime untouched lets BotManager's 2-minute idle floor retry soon instead of
            // going dark for 4 hours on every failure.
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else if (!cardFound)
        {
            // Live-confirmed, 2026-09-26 (Steam-0): the event isn't even in the account's list
            // anymore (outgrown the new-player window) - retrying every 2 minutes forever wastes a
            // full scan cycle for nothing, unlike a genuinely transient failure. Back off to the
            // normal cadence instead; it'll pick back up on its own if the event ever reappears.
            Debug("[INFO] 'New Player Event' isn't in this account's event list - backing off to the normal cadence.");
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else
        {
            Debug("[INFO] AnniversaryShop never opened - see EventManager/OpenEvent debug lines above for why.");
        }

        yield return EventManager.Close;
    }
}
