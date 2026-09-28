using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild.Shop;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using MelonLoader;

namespace Firebot.Tasks.Guild;

/// <summary>Claims the Guild shop's free pickaxes once enough have piled up; MinerQuestTask spends them.</summary>
public class FreePickaxesTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 50;

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

    // The badge isn't used for scheduling: this claims only once pickaxe_claim_threshold is reached.
    public override IEnumerator Execute()
    {
        yield return Notifications.FreePickaxes;

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
