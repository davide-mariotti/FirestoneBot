using System;
using System.Collections;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Character;
using Firebot.GameModel.Primitives;
using Firebot.Utilities;
using MelonLoader;

namespace Firebot.Tasks.Character;

/// <summary>
///     The daily formation check (FORMATION_PLAN.md): the strongest five the account can field now, and
///     the target its own multipliers favor. Logs both against the formation in battle; without
///     dry_run it also switches the leader when the five are already the right ones.
/// </summary>
public class FormationTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    private const double MinGain = 0.10;

    private MelonPreferences_Entry<bool> _dryRun;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_dryRun != null) return;

        _dryRun = category.CreateEntry("dry_run", true, "Dry Run",
            "Only logs the best formation and the target. Set false to let the bot change the leader. Default: true.");
    }

    public override IEnumerator Execute()
    {
        NextRunTime = GameDay.NextReset();

        var read = Heroes.Read();
        if (read == null)
        {
            Debug("[FAILED] Formation: the heroes didn't read - nothing decided.");
            NextRunTime = DateTime.Now.AddHours(1);
            yield break;
        }

        var best = FormationPlanner.Best(read.Heroes, h => h.Offense, read.LeaderRatio);
        var now = FormationPlanner.Score(read.Heroes, read.InBattle, read.Leader, h => h.Offense, read.LeaderRatio);
        var target = FormationPlanner.Best(read.Heroes, h => h.Potential, read.LeaderRatio);
        var change = FormationPlanner.Beats(best.Score, now, MinGain);
        var sameFive = best.Slots.ToHashSet().SetEquals(read.InBattle);

        Debug($"[INFO] Formation: now {string.Join(", ", read.InBattle)} (leader {read.Leader}); " +
              $"best {string.Join(", ", best.Slots)} (leader {best.Leader}, x{Math.Pow(10, best.Score - now):0.00}); " +
              $"target {string.Join(", ", target.Slots)} (leader {target.Leader}); leader ratio {read.LeaderRatio:0.00}; " +
              (!change ? "keep." : _dryRun.Value ? "would change (dry run)." : sameFive ? "changing the leader." :
                  "a different five - not applied yet (FORMATION_PLAN.md, phase 3)."));
        Debug("[INFO] Formation heroes (log10 offense/potential): " + string.Join(", ",
            read.Heroes.OrderByDescending(h => h.Offense).Select(h => $"{h.Name} {h.Offense:0.00}/{h.Potential:0.00}")));

        if (!change || _dryRun.Value || !sameFive || best.Leader == read.Leader) yield break;

        yield return Party.Open;
        yield return Poll.Until(() => Party.FormationNames().Contains(best.Leader), 16);
        yield return Party.SetLeader(best.Leader, line => Debug(line));
        yield return Party.Close;
        Debug($"[INFO] Formation: screens after closing the Party: {Watchdog.DumpActiveScreens()}.");

        var after = Heroes.Read();
        Debug(after?.Leader == best.Leader
            ? $"[INFO] Formation: leader now {after.Leader}."
            : $"[FAILED] Formation: leader still {after?.Leader ?? "unread"}, wanted {best.Leader}.");
    }
}
