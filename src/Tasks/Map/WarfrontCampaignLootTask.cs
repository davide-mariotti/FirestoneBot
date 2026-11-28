using System;
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

    // The loot refills 6 h after the last claim (the game's timestamp + 6 h matched the screen's countdown
    // to the second on Steam-27 and -28, 27/11).
    private static readonly TimeSpan LootInterval = TimeSpan.FromHours(6);

    public override IEnumerator Execute()
    {
        var last = GameState.Get("campaign loot last", () => GameState.H.WFCampaignMissionHandler.lastLootClaimTimestamp);
        if (!IsNotificationVisible() && last is > 0 && GameState.At(last.Value) is { } claimed && claimed + LootInterval > DateTime.Now)
        {
            NextRunTime = claimed + LootInterval;
            Debug($"[INFO] From the game: loot claimed {claimed:HH:mm:ss}, the next at {NextRunTime:HH:mm:ss} - not opening.");
            yield break;
        }

        yield return Notifications.WarfrontCampaign;

        yield return WorldMap.Open;
        yield return WorldMap.OpenWarfrontCampaignTab;

        yield return WarfrontLoot.Claim;
        NextRunTime = WarfrontLoot.NextRunTime;
        // The game's side (WFCampaignMissionHandler), next to the screen's countdown: what a gate from the
        // data needs is the loot's interval, to be read off these lines before it replaces the visit.
        Debug($"[INFO] Campaign loot from the game: claimed {GameState.Get("campaign loot claimed", () => GameState.H.WFCampaignMissionHandler.hasClaimedLoot)}, " +
              $"last {(GameState.Get("campaign loot last", () => GameState.H.WFCampaignMissionHandler.lastLootClaimTimestamp) is { } stamp ? GameState.At(stamp)?.ToString("dd/MM HH:mm:ss") : "-")}, " +
              $"screen next {NextRunTime:dd/MM HH:mm:ss}.");

        yield return WorldMap.Close;
    }
}
