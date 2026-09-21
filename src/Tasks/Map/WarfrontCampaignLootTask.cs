using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Map;
using Firebot.GameModel.Features.Map.WarfrontCampaign;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Map;

// Wired to notification scheduling per the user (2026-09-20) - the earlier caution (this badge
// might also mean "daily/liberation missions ready", not just loot) still stands, but running this
// task's claim-and-check early on that ambiguous signal is harmless (a safe no-op if there's nothing
// to claim yet), and the user wants the badge addressed promptly rather than left lit.
public class WarfrontCampaignLootTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Warfront;
    protected override int MinimumCharacterLevel => 50;

    // Cosmetic only, per the user (2026-09-20) - avoids the class-name-derived "Warfront Campaign
    // Loot" repeating the group name in the terminal table ("Warfront - Warfront Campaign Loot").
    protected override string DisplayName => "Campaign Loot";

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.WarfrontCampaign;

    public override IEnumerator Execute()
    {
        yield return Notifications.WarfrontCampaign;

        // Guaranteed path regardless of the notification - same reasoning as the previous tasks:
        // don't rely on the screen/tab already being open/selected.
        yield return WorldMap.Open;
        yield return WorldMap.OpenWarfrontCampaignTab;

        yield return WarfrontLoot.Claim;
        NextRunTime = WarfrontLoot.NextRunTime;

        yield return WorldMap.Close;
    }
}
