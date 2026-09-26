using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Events;

namespace Firebot.Tasks.Events;

/// <summary>
///     Decorated Heroes (recurring seasonal event, currently active per the user's screenshot,
///     2026-09-23) - Battle -&gt; Events -&gt; "Decorated heroes" -&gt; Challenges (claim all) -&gt;
///     Stars exchange (buy Golden Key with Stars of Recognition). Per the user, deliberately never
///     touches Medals, Skins or Market (chest packs) - only Challenges and Exchange. First concrete
///     event task, built on top of the generic EventManager hub (see Paths.EventManagerLoc's doc
///     comment) so future recurring events (Halloween/Winter/Valentine's/Spring/Tropicana/Space) reuse
///     the same Challenges/Exchange shapes, only needing their own per-event shop screen added.
///     Never automated before. Root screen mounts not yet live-confirmed - see Paths.Events.cs.
/// </summary>
public class DecoratedHeroesEventTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Events;

    // Per the user (2026-09-26): buy Beer with Stars of Recognition instead of Golden key - Beer
    // is more valuable to the account. Exchange list order (1 Legendary chest, 2 Golden chest, 3
    // Beer, 4 Golden key, 5 Contract, 6 Blueprints, 7 Dragon blood) - the buy button's own
    // IsClickable() enforces the "Claimed: X/50" cap, same as everywhere else in this codebase.
    private const string TargetExchangeItem = "Beer";

    // Per the user (2026-09-26): recheck hourly instead of every 4h, so newly-unlocked challenges
    // (they reset periodically per the screenshot's "Challenges will be renewed in: 17:25:05") and
    // exchange purchases happen promptly instead of piling up.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(1);

    public override IEnumerator Execute()
    {
        yield return EventManager.Open;
        Debug($"[INFO] EventManager hub visible after Open: {EventManager.IsVisible}");

        var cardFound = false;
        yield return EventManager.OpenEvent("Decorated heroes", found => cardFound = found);
        yield return DecoratedHeroesShop.WaitUntilOpen();
        Debug($"[INFO] DecoratedHeroesShop visible after OpenEvent: {DecoratedHeroesShop.IsVisible}, cardFound: {cardFound}");

        if (DecoratedHeroesShop.IsVisible)
        {
            yield return DecoratedHeroesShop.OpenChallengesTab;
            yield return DecoratedHeroesShop.ClaimAllChallenges();

            yield return DecoratedHeroesShop.OpenExchangeTab;
            yield return DecoratedHeroesShop.TrySetBestQuantity();
            yield return DecoratedHeroesShop.BuyItem(TargetExchangeItem);

            yield return DecoratedHeroesShop.Close;

            // Only schedule the long recheck on an actual completed pass - same reasoning as
            // ForbiddenKnowledgeTask's own early-bail comment: if the shop never opened (event not
            // running, a stale path, a timing hiccup), leaving NextRunTime untouched lets
            // BotManager's 2-minute idle floor retry soon instead of going dark for 4 hours on
            // every failure - live-confirmed this was masking the events/ root-path fix for hours,
            // 2026-09-23.
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else if (!cardFound)
        {
            // Live-confirmed, 2026-09-26 (Steam-0, New Player Event's identical situation): if the
            // event isn't even in the account's list, retrying every 2 minutes forever wastes a full
            // scan cycle for nothing - back off to the normal cadence instead.
            Debug("[INFO] 'Decorated heroes' isn't in this account's event list - backing off to the normal cadence.");
            NextRunTime = DateTime.Now + RecheckDelay;
        }
        else
        {
            Debug("[INFO] DecoratedHeroesShop never opened - see EventManager/OpenEvent debug lines above for why.");
        }

        yield return EventManager.Close;
    }
}
