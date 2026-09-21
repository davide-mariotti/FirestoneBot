using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild.Shop;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using MelonLoader;

namespace Firebot.Tasks.Guild;

public class FreePickaxesTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 50;

    private MelonPreferences_Entry<int> _pickaxeClaimThreshold;

    public int PickaxeClaimThreshold => _pickaxeClaimThreshold?.Value ?? 1;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_pickaxeClaimThreshold != null) return;

        _pickaxeClaimThreshold = category.CreateEntry(
            "pickaxe_claim_threshold",
            5,
            "Pickaxe Claim Threshold",
            "Minimum number of free pickaxes required before claiming. " +
            "Set to 1 to claim as soon as available, or up to 30 to wait for maximum. " +
            "Default is 5 - per the user (2026-09-20): claims sooner so the notification badge " +
            "doesn't sit lit for as long, instead of waiting for the full 30."
        );
    }

    // No NotificationPath, deliberately - a threshold gate means the badge being up doesn't
    // necessarily mean there's anything worth claiming yet (same reasoning as before).
    public override IEnumerator Execute()
    {
        // Fast path: the notification (when up) opens GuildShop directly on the right tab. Falls
        // through safely if it's not visible.
        yield return Notifications.FreePickaxes;

        // Guaranteed path regardless of the notification, same reasoning as the Store tabs: don't
        // rely on the shop already being open/on the right tab. Every click below is a safe no-op
        // if that step already happened via the notification.
        yield return TownGuild.Open;
        yield return TownGuild.OpenGuildShop;
        yield return GuildShop.OpenSuppliesTab;

        if (FreePickaxes.Quantity >= PickaxeClaimThreshold)
            yield return FreePickaxes.Claim;

        NextRunTime = FreePickaxes.NextRunTime;

        yield return GuildShop.Close;
        yield return TownGuild.Close;
    }
}
