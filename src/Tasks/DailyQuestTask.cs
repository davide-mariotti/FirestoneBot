using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.Core.Tasks;
using Firebot.GameModel.Shared;
using Firebot.Utilities;
using MelonLoader;

namespace Firebot.Tasks;

/// <summary>
///     A daily quest driven by its real progress on the Quests screen, never by a count of its own:
///     reads "done/target", does exactly what's missing, reads again and claims it. Only a quest the
///     screen shows as complete counts as done for the day; one still short (no tokens, pickaxes,
///     chests or items yet) comes back an hour later.
/// </summary>
public abstract class DailyQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromHours(1);
    private static readonly TimeSpan StaleWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan StaleRetry = TimeSpan.FromMinutes(5);

    /// <summary>The quest card's name on the Daily tab ("Miner").</summary>
    protected abstract string QuestName { get; }

    /// <summary>Up to `missing` steps of the quest, ending with its own screens closed.</summary>
    protected abstract IEnumerator Work(int missing);

    /// <summary>For a task's own settings; called before last_done_date is created.</summary>
    protected virtual void OnConfigureQuest(MelonPreferences_Category category) { }

    private MelonPreferences_Entry<string> _lastDoneDate;

    protected bool IsDoneToday => _lastDoneDate?.Value == GameDay.Today();

    // Each loaded quest task, for IsQuestDoneToday.
    private static readonly Dictionary<Type, DailyQuestTask> Loaded = new();

    /// <summary>
    ///     Whether T's quest showed complete today. An event challenge that spends the same resource
    ///     waits for it, so the daily quest - it unlocks the weeklies - always comes first.
    /// </summary>
    public static bool IsQuestDoneToday<T>() where T : DailyQuestTask =>
        Loaded.TryGetValue(typeof(T), out var task) && task.IsDoneToday;

    protected sealed override void OnConfigure(MelonPreferences_Category category)
    {
        if (_lastDoneDate != null) return;

        Loaded[GetType()] = this;
        OnConfigureQuest(category);

        _lastDoneDate = category.CreateEntry(
            "last_done_date",
            "",
            "Last Done Date",
            "(auto-managed, don't edit) - the last game-day the Quests screen showed this quest complete. " +
            "Skips the task until the next reset at 10:00."
        );
    }

    public sealed override IEnumerator Execute()
    {
        if (IsDoneToday)
        {
            NextRunTime = GameDay.NextReset();
            yield break;
        }

        var before = (Listed: false, Done: -1, Target: -1);
        yield return CharacterScreen.CheckDailyQuest(QuestName, r => before = r);

        // Right after 10:00 the Quests screen can still show yesterday's finished quest for a minute
        // or more (01/10: Collector on 9 instances, every quest on Steam-13 and -16, up to 10:01).
        if (before.Listed && before.Target > 0 && before.Done >= before.Target &&
            DateTime.Now - GameDay.NextReset().AddDays(-1) < StaleWindow)
        {
            Debug($"[INFO] '{QuestName}' quest: {before.Done}/{before.Target} right after the reset - read again later.");
            NextRunTime = DateTime.Now + StaleRetry;
            yield break;
        }

        var after = before;
        if (before.Listed && before.Target > 0 && before.Done < before.Target)
        {
            yield return Work(before.Target - Math.Max(before.Done, 0));
            yield return CharacterScreen.CheckDailyQuest(QuestName, r => after = r);
        }

        Debug($"[INFO] '{QuestName}' quest: {before.Done}/{before.Target} -> {after.Done}/{after.Target}" +
              (after.Listed ? "." : " (not on the Daily tab)."));

        if (after.Listed && after.Target > 0 && after.Done >= after.Target)
        {
            if (_lastDoneDate != null) _lastDoneDate.Value = GameDay.Today();
            NextRunTime = GameDay.NextReset();
        }
        else
            NextRunTime = DateTime.Now + RetryDelay;
    }
}
