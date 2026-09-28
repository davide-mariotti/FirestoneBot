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
///     Chaos Rift: attacks the server boss with the free Moon Stones (auto-hit on, bulk hits when
///     offered), then spends the Dark Rune earned on Tomes of Power for ForbiddenKnowledgeTask. The
///     shop's other tabs cost real money and are never opened. The ChaosRift badge never clears; the
///     scheduler's round-robin keeps it from crowding out other tasks.
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
        yield return Notifications.ChaosRift;
        yield return Notifications.ChaosRiftSupplies;

        // The supplies badge opens the shop directly, skipping the boss screen - so both are checked.
        yield return TownGuild.Open;
        yield return TownGuild.OpenChaosRift;

        if (ChaosRift.IsVisible)
        {
            yield return ChaosRift.EnsureAutoHitOn();

            yield return ChaosRift.TrySetBestQuantity();

            var hitThisRun = false;
            for (var i = 0; i < MaxHits; i++)
            {
                if (!ChaosRift.HitBtn.IsClickable()) break;
                yield return ChaosRift.HitBtn.Click();
                hitThisRun = true;
            }

            // Leaving before the last hit's animation ends cuts it short.
            if (hitThisRun) yield return ChaosRift.WaitForHitResult();

            yield return ChaosRift.OpenShop;
        }

        yield return ChaosRiftShop.WaitUntilOpen();

        if (ChaosRiftShop.IsVisible)
        {
            yield return ChaosRiftShop.OpenSuppliesTab;

            // Tomes of Power only - never Eclipse Stones (a standing rule of the user's).
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
