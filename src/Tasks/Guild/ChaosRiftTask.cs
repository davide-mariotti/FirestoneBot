using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Chaos Rift: attacks the server boss with the free Moon Stones (bulk hits when offered), then
///     spends the Dark Rune earned on Tomes of Power for ForbiddenKnowledgeTask. The shop's other tabs
///     cost real money and are never opened. The ChaosRift badge never clears; the scheduler's
///     round-robin keeps it from crowding out other tasks. The auto-hit button is left alone: it's a
///     plain button with no readable state (hidden on Steam-0), so clicking it would only flip it.
/// </summary>
public class ChaosRiftTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 100;

    protected override string[] NotificationPaths =>
        RailBadges(Paths.BattleLoc.NotificationsLoc.ChaosRift, Paths.BattleLoc.NotificationsLoc.ChaosRiftSupplies);

    // Moon Stones recharge once a day.
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(24);

    // A ceiling only: HitBtn stops being clickable when the stones run out.
    private const int MaxHits = 20;

    private const int MaxShopBuysPerItem = 30;

    public override IEnumerator Execute()
    {
        // No badge, the god not attackable now or no token to hit with (the game's own checks), and the
        // Dark Runes short of a Tome of Power: Chaos Rift has nothing for this task.
        var canHit = GameState.Get("chaos rift hit", () =>
            GameState.H.ChaosRiftHandler.CanAttackGod && Il2Cpp.GameInitialize.MenuLoader.ChaosRiftMech.CanAttack(1));
        var runes = GameState.Currency(Il2Cpp.CurrencyCode.DarkRune);
        // The shop's price of a tome (it grows with each one bought): the game's own figure reads 0 until the
        // shop has been shown (27/11, 17839 once open), so the last one seen there stands in.
        var now = GameState.Get("tome of power cost", () => (double)Il2Cpp.ChaosRiftShopSuppliesInteraction.GetTomeExchangeCostMultiple());
        double? tome = now > 0 ? now : _lastTomeCost;
        if (!IsNotificationVisible() && canHit == false && runes < tome)
        {
            Debug($"[INFO] From the game: no hit to make, {runes:0} Dark Runes for a {tome:0} tome - not opening.");
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }


        yield return Notifications.ChaosRift;
        yield return Notifications.ChaosRiftSupplies;

        // The supplies badge opens the shop directly, skipping the boss screen - so both are checked.
        yield return TownGuild.Open;
        yield return TownGuild.OpenChaosRift;

        if (ChaosRift.IsVisible)
        {
            yield return ChaosRift.TrySetBestQuantity();

            var hits = 0;
            for (; hits < MaxHits; hits++)
            {
                // The button is unclickable while a hit plays (2/10: one hit per run), as in ArcaneCrystal.Hit.
                yield return Poll.Until(() => ChaosRift.HitBtn.IsClickable(), 20, 0.3f);
                if (!ChaosRift.HitBtn.IsClickable()) break;
                yield return ChaosRift.HitBtn.Click();
            }

            Debug($"[INFO] Chaos Rift: {hits} hit(s).");

            // Leaving before the last hit's animation ends cuts it short.
            if (hits > 0) yield return ChaosRift.WaitForHitResult();

            yield return ChaosRift.OpenShop;
        }

        yield return ChaosRiftShop.WaitUntilOpen();

        if (ChaosRiftShop.IsVisible)
        {
            yield return ChaosRiftShop.OpenSuppliesTab;

            // Tomes of Power only - never Eclipse Stones (a standing rule of the user's).
            var before = GameState.Currency(Il2Cpp.CurrencyCode.DarkRune);
            var shopCost = GameState.Get("tome shop cost", () =>
                Il2Cpp.ChaosRiftShopSuppliesInteraction.GetTomeExchangeCostMultiple());
            yield return BuyWhileAffordable(ChaosRiftShop.TomeOfPowerBuyBtn);
            Debug($"[INFO] Chaos Rift shop: a tome costs {shopCost?.ToString() ?? "-"}, Dark Runes {before:0} -> " +
                  $"{GameState.Currency(Il2Cpp.CurrencyCode.DarkRune):0}.");
            // What the next visit is weighed against, read again after any purchase.
            if (GameState.Get("tome shop cost", () => Il2Cpp.ChaosRiftShopSuppliesInteraction.GetTomeExchangeCostMultiple()) is > 0 and var cost)
                _lastTomeCost = cost;

            yield return ChaosRiftShop.Close;
        }

        yield return ChaosRift.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private static double? _lastTomeCost;

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
