using System;
using System.Collections;
using Firebot.Tasks.Guild;
using Firebot.Tasks.Inventory;
using Firebot.Tasks.Town;
using Logger = Firebot.Core.Logger;

namespace Firebot.Tasks.Events;

/// <summary>
///     Does exactly what an event challenge is missing - no more, and no reserve kept - with the
///     daily quest tasks' own steps. An action spending a daily quest's resource waits until that
///     quest is done today: the quest comes first, and its steps count for the challenge too.
/// </summary>
public static class EventChallengeActions
{
    /// <summary>onDone gets the steps actually done; it isn't called when the action waits or doesn't exist.</summary>
    public static IEnumerator Complete(Challenge challenge, Action<int> onDone) => challenge.Kind switch
    {
        ChallengeKind.CrystalHits => AfterQuest<MinerQuestTask>(() => MinerQuestTask.HitCrystal(challenge.Missing, onDone)),
        ChallengeKind.TavernPlays => AfterQuest<GamerQuestTask>(() => GamerQuestTask.Play(challenge.Missing, onDone)),
        ChallengeKind.OpenChests => AfterQuest<CollectorQuestTask>(() => CollectorQuestTask.OpenChests(challenge.Missing, onDone)),
        ChallengeKind.SellItems => AfterQuest<MerchantQuestTask>(() => MerchantQuestTask.Sell(challenge.Missing, onDone)),
        ChallengeKind.TreeOfLifeUpgrades => TreeOfLifeTask.Buy(challenge.Missing, onDone),
        ChallengeKind.MeteoriteResearches => MeteoriteResearchTask.Research(challenge.Missing, onDone),
        _ => null
    };

    private static IEnumerator AfterQuest<T>(Func<IEnumerator> action) where T : DailyQuestTask
    {
        if (DailyQuestTask.IsQuestDoneToday<T>())
            yield return action();
        else
            Logger.Debug($"[INFO] Event challenge: waits for today's {typeof(T).Name} to finish first.");
    }
}
