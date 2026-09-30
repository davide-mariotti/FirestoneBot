using System;
using System.Collections;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
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
        ChallengeKind.EnlightenGuardians => GuardianTrainingTask.Enlighten(challenge.Missing, onDone),
        ChallengeKind.ExoticUpgrades => MerchantQuestTask.Upgrade(challenge.Missing, onDone),
        ChallengeKind.GuildDonation => Donate(challenge.Missing, onDone),
        _ => null
    };

    /// <summary>
    ///     One donation. Exactly the missing coins can't be given: the game's smallest donation is
    ///     1.000, or every coin when fewer are left ("Max"), so a 500 challenge costs one of those.
    ///     onDone gets the coins actually donated.
    /// </summary>
    private static IEnumerator Donate(int missing, Action<int> onDone)
    {
        yield return TownGuild.Open;
        yield return TownGuild.OpenBank;
        yield return Poll.Until(() => GuildBank.IsVisible);

        var before = GuildBank.CoinCount;
        var icon = GuildBank.CoinIcon;
        var button = GuildBank.Donate1kBtn.IsClickable() ? GuildBank.Donate1kBtn : GuildBank.DonateAllBtn;
        if (icon == "guildCoin64" && before > 0 && button.IsClickable())
        {
            yield return button.Click();
            yield return Poll.Until(() => GuildBank.CoinCount < before);

            var after = GuildBank.CoinCount;
            Logger.Debug($"[INFO] Guild donation for {missing} missing: guild coins {before} -> {after}.");
            if (after < before) onDone(before - after);
        }
        else
            Logger.Debug($"[INFO] Guild donation for {missing} missing: nothing to give ({before} coins, icon '{icon}').");

        yield return GuildBank.Close;
        yield return TownGuild.Close;
    }

    private static IEnumerator AfterQuest<T>(Func<IEnumerator> action) where T : DailyQuestTask
    {
        if (DailyQuestTask.IsQuestDoneToday<T>())
            yield return action();
        else
            Logger.Debug($"[INFO] Event challenge: waits for today's {typeof(T).Name} to finish first.");
    }
}
