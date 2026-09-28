using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Map;
using Firebot.GameModel.Features.Map.WarfrontCampaign;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Map;

/// <summary>
///     Collects the Warfront campaign loot. Its badge may also mean daily missions are waiting - running
///     then is harmless, the claim is a no-op when there's nothing to collect.
/// </summary>
public class WarfrontCampaignLootTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Warfront;
    protected override int MinimumCharacterLevel => 50;

    protected override string DisplayName => "Campaign Loot";

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.WarfrontCampaign;

    public override IEnumerator Execute()
    {
        yield return Notifications.WarfrontCampaign;

        yield return WorldMap.Open;
        yield return WorldMap.OpenWarfrontCampaignTab;

        yield return WarfrontLoot.Claim;
        NextRunTime = WarfrontLoot.NextRunTime;

        yield return WorldMap.Close;
    }
}
