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

    // Per the user (2026-09-23): buy Golden Key with Stars of Recognition - the wiki/screenshot
    // shows a "Claimed: X/50" cap enforced by the buy button's own IsClickable(), same as everywhere
    // else in this codebase.
    private const string TargetExchangeItem = "Golden key";

    // Recurring event, no fixed daily reset like a quest - recheck a few times a day so new
    // challenges (they reset periodically per the screenshot's "Challenges will be renewed in:
    // 17:25:05") get claimed without needing to wait a full day.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(4);

    public override IEnumerator Execute()
    {
        yield return EventManager.Open;
        yield return EventManager.OpenEvent("Decorated heroes");
        yield return DecoratedHeroesShop.WaitUntilOpen();

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

        yield return EventManager.Close;
    }
}
