using System;
using System.Collections;
using Firebot.BotActions;
using Firebot.Core.Tasks;
using Firebot.Utilities;
using MelonLoader;

namespace Firebot.Tasks.Town;

/// <summary>
///     The daily push at the wall (the user's request, 28/11): once a game day, with AutoRetreat at the
///     wall, the three scrolls and the five battle items, then the climb (GAMESTATE_PLAN.md, "Routine di
///     push con pergamene e consumabili"). A task, not part of AutoRetreat's loop, so the scheduler keeps
///     it apart from the other tasks' screens. Off by default; for now only "dry": the inventory's slots
///     and use buttons logged, nothing used.
/// </summary>
public class DailyPushTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    protected override bool DefaultEnabled => false;

    private MelonPreferences_Entry<string> _mode;
    private MelonPreferences_Entry<string> _lastDay;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_mode != null) return;

        _mode = category.CreateEntry("mode", "dry", "Mode",
            "'dry' (default): at the wall once a game day, opens the inventory and logs the scrolls' and items' " +
            "slots and use buttons; nothing is used. 'on': at the farm stage, the three scrolls, Drums of War, " +
            "Dragon Armor, Guardian's Rune, both Totems and one of each instant gold item (Pouch, Bucket, Crate, " +
            "Pile), then the climb to the top and the boss. Never Midas' Touch.");
        _lastDay = category.CreateEntry("last_day_internal", "", "Last Day (internal)",
            "(auto-managed, don't edit) - the game day of the last push.");
    }

    /// <summary>Today's push is still to come ("on"): EmpowerTask's growth rule waits for it at the wall.</summary>
    public static bool Waiting { get; private set; }

    public override IEnumerator Execute()
    {
        var on = _mode.Value?.Trim() == "on";
        Waiting = on && _lastDay.Value != GameDay.Today();
        if (_lastDay.Value == GameDay.Today())
        {
            NextRunTime = GameDay.NextReset();
            yield break;
        }

        // "dry" only looks at the inventory, "test" uses one scroll and one item: any time.
        if (!on)
        {
            _lastDay.Value = GameDay.Today();
            NextRunTime = GameDay.NextReset();
            yield return _mode.Value?.Trim() == "test" ? DailyPush.Test() : DailyPush.Discover();
            yield break;
        }

        if (!AutoRetreat.HitWall || AutoRetreat.FarmSearching)
        {
            // At the wall it's a minute: the push must come before the Empower (-27 empowered 20 s after its search, 28/11 04:52).
            Debug($"[INFO] Daily push: not farming at the wall yet - looking again in {(AutoRetreat.HitWall ? 1 : 5)} min.");
            NextRunTime = DateTime.Now.AddMinutes(AutoRetreat.HitWall ? 1 : 5);
            yield break;
        }

        _lastDay.Value = GameDay.Today();
        Waiting = false;
        NextRunTime = GameDay.NextReset();

        // The heroes' upgrades spend the gold once the task ends; AutoRetreat climbs and fights meanwhile.
        var ok = false;
        yield return DailyPush.Run(true, r => ok = r);
        if (DailyPush.Used > 0) AutoRetreat.RequestPush();
        if (!ok) Debug("[FAILED] Daily push: not everything went through - see the [DailyPush] lines.");
    }
}
