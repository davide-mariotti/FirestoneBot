using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using UnityEngine.UI;
using Logger = Firebot.Core.Logger;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Chaos Rift (Town -&gt; Guild -&gt; Chaos Rift, level 100) - attacks the shared server boss with
///     whatever free Moon Stones are on hand (cap 10/day), then spends any Dark Rune earned from that
///     damage ONLY on Tomes of Power (feeds ForbiddenKnowledgeTask) in the Supplies shop - explicit
///     standing instruction from the user (2026-09-20): never buy Eclipse Stone, even though the
///     Supplies shop also offers it (see ChaosRiftShopLoc.MoonstoneOneBuyBtn/BulkBuyBtn, deliberately
///     unwired). Never touches the "Upgrades" button (its target screen, per
///     a research pass, looks like the generic Gold-based hero/guardian leveling screen reused from
///     elsewhere - not clearly Chaos-Rift-specific, and spending Gold automatically here wasn't asked
///     for), the "Competition" button (read-only leaderboard, rewards are mailed automatically at
///     month end per the wiki, nothing to claim there), or the Shop's Sale/Daily Deals/Weekly Deals
///     tabs (real money per the wiki + the dump's own realCurrencyIcon field name).
///     Uses both the "ChaosRift" and "ChaosRiftSupplies" badges for scheduling - the latter added
///     2026-09-20 per the user, confirmed lit on -0 (signals Dark Rune ready to spend, matching the
///     shop-buy step already handled below). "ChaosRift" is unusual: unlike every other badge in this
///     codebase, it never seems to clear even right after successfully hitting the god and buying
///     everything affordable. Originally left unwired for exactly that reason (it would have made
///     this task perpetually "ready" every scan cycle, starving ForbiddenKnowledgeTask - the same
///     Guild-group scheduler slot - of ever running). Now safe: BotManager.PickNotificationTask
///     round-robins fairly among however many tasks have a visible badge at once, so an always-lit
///     badge only wins its fair share of ticks instead of every single one.
///     This whole feature is new, requested by the user, never automated before. All paths were fully
///     live-confirmed, 2026-09-20, across several rounds of diagnostics - the one real surprise: the
///     interactive layer (hitButton, actionButtons, autoHitToggle) isn't directly under the screen's
///     own root as a first UnityPy dump research pass suggested, it's nested under a "UIElements"
///     wrapper (godParent/hitButton specifically) - see ChaosRiftLoc for the confirmed paths. Also
///     confirmed: ChaosRiftShop only comes into existence as a real GameObject the first time "shop" is
///     ever successfully clicked (every path under it read as "path broken", not just "hidden", until
///     that first click actually landed) - the first live run mistook this for a wrong path guess.
/// </summary>
public class ChaosRiftTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 100;

    protected override string[] NotificationPathCandidates => new[]
    {
        Paths.BattleLoc.NotificationsLoc.Root + "/" + Paths.BattleLoc.NotificationsLoc.ChaosRift,
        Paths.BattleLoc.NotificationsLoc.FallbackRoot + "/" + Paths.BattleLoc.NotificationsLoc.ChaosRift,
        Paths.BattleLoc.NotificationsLoc.Root + "/" + Paths.BattleLoc.NotificationsLoc.ChaosRiftSupplies,
        Paths.BattleLoc.NotificationsLoc.FallbackRoot + "/" + Paths.BattleLoc.NotificationsLoc.ChaosRiftSupplies
    };

    // Moon Stones recharge once per day (cap 10) - no finer-grained cooldown to track, same
    // "once a day is close enough" reasoning as Daily Store Offers/Quests.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(24);

    // Generous ceiling, not a real cap - the daily Moon Stone cap (10) plus slack for any Eclipse
    // Stones already on hand; HitBtn's own IsClickable() is what actually stops the loop.
    private const int MaxHits = 20;

    // Same "keep going until nothing's left to buy" ceiling used by BeerExchange/TreeOfLife.
    private const int MaxShopBuysPerItem = 30;

    public override IEnumerator Execute()
    {
        yield return Notifications.ChaosRift;
        yield return Notifications.ChaosRiftSupplies;

        // Live-confirmed, 2026-09-20: the "ChaosRiftSupplies" badge's own quick-access click jumps
        // straight to the Supplies shop, skipping the main Chaos Rift screen entirely - a previous
        // version only ever checked ChaosRift.IsVisible and bailed out immediately whenever that
        // badge fired, never even attempting the shop purchase despite the shop being genuinely open
        // right there (confirmed by the user's own screenshot). Both entry shapes are handled below
        // instead of assuming one specific screen is active after the notification/navigation clicks.
        yield return TownGuild.Open;
        yield return TownGuild.OpenChaosRift;

        if (ChaosRift.IsVisible)
        {
            yield return ChaosRift.EnsureAutoHitOn();

            // Per the user (2026-09-23): use the bulk-hit multiplier when available instead of
            // clicking one at a time - see ChaosRift.TrySetBestQuantity. Falls back to individual
            // clicks if none of the known candidate values turn up (not fully confirmed live).
            yield return ChaosRift.TrySetBestQuantity();

            var hitThisRun = false;
            for (var i = 0; i < MaxHits; i++)
            {
                if (!ChaosRift.HitBtn.IsClickable()) break;
                yield return ChaosRift.HitBtn.Click();
                hitThisRun = true;
            }

            // Per the user (2026-09-23): wait for the last hit's animation to actually resolve
            // before navigating to the shop - previously this moved on immediately, which cut the
            // animation short (same class of bug Awakening's own AwakenAnimationWait was added for).
            if (hitThisRun) yield return ChaosRift.WaitForHitResult();

            yield return ChaosRift.OpenShop;
        }

        yield return ChaosRiftShop.WaitUntilOpen();

        if (ChaosRiftShop.IsVisible)
        {
            yield return ChaosRiftShop.OpenSuppliesTab;

            // Per the user (2026-09-20): ONLY Tome of Power, never Eclipse Stone - not a wording
            // nuance, an explicit standing instruction not to spend Dark Rune on Eclipse Stone.
            yield return BuyWhileAffordable(ChaosRiftShop.TomeOfPowerBuyBtn);

            yield return ChaosRiftShop.Close;
        }

        yield return ChaosRift.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private static IEnumerator BuyWhileAffordable(GameButton buyBtn)
    {
        for (var i = 0; i < MaxShopBuysPerItem; i++)
        {
            if (!buyBtn.IsClickable()) break;
            yield return buyBtn.Click();

            if (!CurrencyMissingPopup.IsShowing) continue;
            yield return CurrencyMissingPopup.Close;
            yield break;
        }
    }
}
