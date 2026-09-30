using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild.Shop;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Claims the Guild shop's free pickaxes once enough have piled up; MinerQuestTask spends them.
///     Badge only: the badge opens the shop on its Supplies tab, while the Guild -> Guild Shop route
///     never reached the timer (1,015 of 1,038 runs in the 26-29/09 logs, each retrying 2 min later).
/// </summary>
public class FreePickaxesTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 50;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.FreePickaxes;

    private MelonPreferences_Entry<int> _pickaxeClaimThreshold;

    private int PickaxeClaimThreshold => _pickaxeClaimThreshold?.Value ?? 1;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_pickaxeClaimThreshold != null) return;

        _pickaxeClaimThreshold = category.CreateEntry(
            "pickaxe_claim_threshold",
            5,
            "Pickaxe Claim Threshold",
            "Minimum number of free pickaxes required before claiming. " +
            "Set to 1 to claim as soon as available, or up to 30 to wait for maximum. " +
            "Default is 5, so the badge doesn't stay lit for long."
        );
    }

    public override IEnumerator Execute()
    {
        // Runs only on the badge, never on a timer.
        NextRunTime = DateTime.MaxValue;

        yield return Notifications.FreePickaxes;
        yield return GuildShop.OpenSuppliesTab;

        Debug($"[INFO] Free pickaxes: {FreePickaxes.Quantity} ('{FreePickaxes.QuantityText}'), threshold {PickaxeClaimThreshold}.");
        if (FreePickaxes.Quantity >= PickaxeClaimThreshold)
            yield return FreePickaxes.Claim;

        yield return GuildShop.Close;
    }
}
