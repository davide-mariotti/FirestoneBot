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
        // The game's side (WFCampaignMissionHandler), next to the screen's countdown: what a gate from the
        // data needs is the loot's interval, to be read off these lines before it replaces the visit.
        Debug($"[INFO] Campaign loot from the game: claimed {GameState.Get("campaign loot claimed", () => GameState.H.WFCampaignMissionHandler.hasClaimedLoot)}, " +
              $"last {(GameState.Get("campaign loot last", () => GameState.H.WFCampaignMissionHandler.lastLootClaimTimestamp) is { } last ? GameState.At(last)?.ToString("dd/MM HH:mm:ss") : "-")}, " +
              $"screen next {NextRunTime:dd/MM HH:mm:ss}.");

        yield return WorldMap.Close;
    }
}
