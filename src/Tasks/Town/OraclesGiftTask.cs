using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Oracle;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Town;

/// <summary>Claims the daily Oracle's Gift once the character reaches the level it unlocks at.</summary>
public class OraclesGiftTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 200;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.OraclesGift;

    public override IEnumerator Execute()
    {
        yield return Notifications.OraclesGift;
        yield return OracleStore.ClaimGift;
        NextRunTime = OracleStore.NextRunTime;
        yield return OracleStore.Close;
    }
}
