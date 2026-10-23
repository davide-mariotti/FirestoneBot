using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Character;
using Firebot.Utilities;

namespace Firebot.Tasks.Character;

/// <summary>
///     The daily formation check (FORMATION_PLAN.md): the strongest five the account can field now, and
///     the target its own multipliers favor. Dry run: only logs both against the formation in battle.
/// </summary>
public class FormationTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    private const double MinGain = 0.10;

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
        var gain = Math.Pow(10, best.Score - now);

        Debug($"[INFO] Formation: now {string.Join(", ", read.InBattle)} (leader {read.Leader}); " +
              $"best {string.Join(", ", best.Slots)} (leader {best.Leader}, x{gain:0.00}); " +
              $"target {string.Join(", ", target.Slots)} (leader {target.Leader}); leader ratio {read.LeaderRatio:0.00}; " +
              (FormationPlanner.Beats(best.Score, now, MinGain) ? "would change (dry run)." : "keep."));
        Debug("[INFO] Formation heroes (log10 offense/potential): " + string.Join(", ",
            read.Heroes.OrderByDescending(h => h.Offense).Select(h => $"{h.Name} {h.Offense:0.00}/{h.Potential:0.00}")));
        yield break;
    }
}
