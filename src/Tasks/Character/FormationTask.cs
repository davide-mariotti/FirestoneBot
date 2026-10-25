using System;
using System.Collections;
using System.Collections.Generic;
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
///     dry_run it swaps heroes from the deck and sets the leader. Runs an hour after the reset (GameDay),
///     out of its rush.
/// </summary>
public class FormationTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    private const double MinGain = 0.10;

    // The target only moves for a clearly better one: investments follow it (HallOfHeroesTask).
    private const double TargetMinGain = 0.20;

    // UTC, an hour after the bot's reset: 11:02 in summer time, 10:02 in winter - always 58 minutes
    // before the restart that Firestone_Riavvio runs at 10:00 UTC.
    private static readonly TimeSpan RunAt = new(9, 2, 0);

    private MelonPreferences_Entry<bool> _dryRun;
    private MelonPreferences_Entry<string> _target;

    /// <summary>The saved target's heroes, which HallOfHeroesTask invests in first. Empty until the first run.</summary>
    public static HashSet<string> Target { get; private set; } = new();

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_dryRun != null) return;

        _dryRun = category.CreateEntry("dry_run", true, "Dry Run",
            "Only logs the best formation and the target. Set false to let the bot change heroes and leader. Default: true.");

        _target = category.CreateEntry("target", "", "Target",
            "(auto-managed, don't edit) - the target team, \"name, name, ... | leader\"; replaced only by one 20% better.");
        Target = TargetNames(_target.Value);
    }

    private static HashSet<string> TargetNames(string saved) =>
        saved.Split('|')[0].Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToHashSet();

    private static DateTime NextRun() => GameDay.NextUtc(RunAt);

    public override IEnumerator Execute()
    {
        NextRunTime = NextRun();

        var read = Heroes.Read();
        if (read == null)
        {
            Debug("[FAILED] Formation: the heroes didn't read - nothing decided.");
            NextRunTime = DateTime.Now.AddHours(1);
            yield break;
        }

        double Value(HeroStats h) => h.Offense;
        var best = FormationPlanner.Best(read.Heroes, Value, read.LeaderRatio);
        var now = FormationPlanner.Score(read.Heroes, read.InBattle, read.Leader, Value, read.LeaderRatio);
        var target = FormationPlanner.Best(read.Heroes, h => h.Potential, read.LeaderRatio);
        var change = FormationPlanner.Beats(best.Score, now, MinGain);

        // The saved target stays unless the new one beats it by TargetMinGain on potential.
        var saved = _target.Value.Split('|');
        var savedNames = TargetNames(_target.Value);
        var savedLeader = saved.Length > 1 ? saved[1].Trim() : "";
        var savedValid = savedNames.Count == FormationPlanner.Size && savedNames.All(n => read.Heroes.Any(h => h.Name == n));
        var savedScore = savedValid ? FormationPlanner.Score(read.Heroes, savedNames, savedLeader, h => h.Potential, read.LeaderRatio)
            : double.NegativeInfinity;
        var targetMoved = FormationPlanner.Beats(target.Score, savedScore, TargetMinGain);
        if (targetMoved) _target.Value = $"{string.Join(", ", target.Slots)} | {target.Leader}";
        else target = new Formation(saved[0].Split(',').Select(n => n.Trim()).ToList(), savedLeader, savedScore);
        Target = TargetNames(_target.Value);

        Debug($"[INFO] Formation: now {string.Join(", ", read.InBattle)} (leader {read.Leader}, score {now:0.00}); " +
              $"best {string.Join(", ", best.Slots)} (leader {best.Leader}, score {best.Score:0.00}, x{Math.Pow(10, best.Score - now):0.00}); " +
              $"target {string.Join(", ", target.Slots)} (leader {target.Leader}); target {(targetMoved ? "new" : "kept")}; " +
              $"leader ratio {read.LeaderRatio:0.00}; " +
              (!change ? "keep." : _dryRun.Value ? "would change (dry run)." : "changing."));
        Debug("[INFO] Formation dps check: " + Heroes.DpsCheck());
        Debug("[INFO] Formation heroes (log10 offense/potential): " + string.Join(", ",
            read.Heroes.OrderByDescending(Value).Select(h => $"{h.Name} {h.Offense:0.00}/{h.Potential:0.00}")));

        yield return Party.Open;
        yield return Poll.Until(() => Party.FormationNames().Count >= Party.FormationSize, 16);
        // The deck fills in after the slots (Steam-15, 23/10: read empty right after them).
        yield return Poll.Until(() => Party.DeckSlots().Count >= Party.FormationSize, 16);
        var deck = Party.DeckSlots();
        Debug($"[INFO] Formation deck: {string.Join(", ", deck.Select(d => d.Portrait + (d.Deployed ? "*" : "")))}.");

        if (change && !_dryRun.Value) yield return Apply(read, best, deck);

        yield return Party.Close;
        if (!change || _dryRun.Value) yield break;

        Debug($"[INFO] Formation: screens after closing the Party: {Watchdog.DumpActiveScreens()}.");
        var after = Heroes.Read();
        var score = after == null ? double.NaN
            : FormationPlanner.Score(after.Heroes, after.InBattle, after.Leader, Value, after.LeaderRatio);
        Debug(after == null
            ? "[FAILED] Formation: the heroes didn't read after the change."
            : $"[INFO] Formation: after {string.Join(", ", after.InBattle)} (leader {after.Leader}, score {score:0.00}, " +
              $"x{Math.Pow(10, score - now):0.00} on before); " +
              (after.InBattle.ToHashSet().SetEquals(best.Slots) && after.Leader == best.Leader ? "as planned." : "[FAILED] not as planned."));
    }

    private IEnumerator Apply(Heroes.Snapshot read, Formation best, List<(UnityEngine.Transform Cell, string Portrait, bool Deployed)> deck)
    {
        // ponytail: deck cells are matched by their portrait sprite; a roster whose sprites don't name the
        // hero would need the cell -> hero map from PartyMech instead.
        UnityEngine.Transform CellOf(string name) => FormationPlanner.MatchPortrait(name, deck.Select(d => d.Portrait).ToList()) is { } i
            ? deck[i].Cell
            : null;

        var outs = read.InBattle.Except(best.Slots).ToList();
        var ins = best.Slots.Except(read.InBattle).ToList();
        for (var i = 0; i < Math.Min(outs.Count, ins.Count); i++)
        {
            var (outCell, inCell) = (CellOf(outs[i]), CellOf(ins[i]));
            if (outCell == null || inCell == null)
            {
                Debug($"[FAILED] Formation: no deck cell for {(outCell == null ? outs[i] : ins[i])} - stopping the swaps.");
                yield break;
            }

            var ok = false;
            yield return Party.Swap(outs[i], ins[i], outCell, inCell, line => Debug(line), result => ok = result);
            if (!ok) yield break;
            Debug($"[INFO] Formation: swapped {outs[i]} -> {ins[i]}.");
        }

        if (Heroes.Read()?.Leader != best.Leader) yield return Party.SetLeader(best.Leader, line => Debug(line));
    }
}
