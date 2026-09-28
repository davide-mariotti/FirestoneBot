using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Town;

/// <summary>
///     Claims every completed daily and weekly quest. One "Quests" badge covers both, but this also
///     checks back at least hourly in case the badge or the renew time misleads.
/// </summary>
public class QuestsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;

    private static readonly TimeSpan FallbackRetryDelay = TimeSpan.FromHours(12);
    private static readonly TimeSpan MaxRecheckDelay = TimeSpan.FromHours(1);

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Quests;

    public override IEnumerator Execute()
    {
        yield return Notifications.Quests;
        yield return CharacterScreen.Open();
        yield return CharacterScreen.OpenQuestsTab;

        yield return CharacterScreen.OpenDailyQuestsSubTab;
        foreach (var claimButton in CharacterScreen.DailyQuestClaimButtons())
            yield return claimButton.Click();
        var dailyRenewTime = CharacterScreen.QuestsRenewTime;

        yield return CharacterScreen.OpenWeeklyQuestsSubTab;
        foreach (var claimButton in CharacterScreen.WeeklyQuestClaimButtons())
            yield return claimButton.Click();
        var weeklyRenewTime = CharacterScreen.QuestsRenewTime;

        yield return CharacterScreen.Close;

        var earliestRenew = dailyRenewTime < weeklyRenewTime ? dailyRenewTime : weeklyRenewTime;
        var computedNext = earliestRenew > DateTime.Now ? earliestRenew : DateTime.Now + FallbackRetryDelay;
        var maxNext = DateTime.Now + MaxRecheckDelay;
        NextRunTime = computedNext < maxNext ? computedNext : maxNext;
    }
}
