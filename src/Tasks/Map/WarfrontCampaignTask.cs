using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Map;
using Firebot.GameModel.Features.Map.WarfrontCampaign;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using MelonLoader;
using Logger = Firebot.Core.Logger;
using CampaignBattle = Firebot.GameModel.Features.Map.WarfrontCampaign.WarfrontCampaign.Battle;

namespace Firebot.Tasks.Map;

/// <summary>
///     Pushes the Warfront campaign on (CAMPAIGN_PLAN.md): every mission/difficulty the squad's battle
///     power reaches, lowest requirement first, until the first defeat. The game's requirement only
///     allows a fight - Easy 32 was lost at 1.88x on Steam-0 (03/10) - so a defeat is retried only
///     at 5% more power or after 24 h, and meanwhile nothing weaker against its enemy is tried either.
///     Each win is a star and a one-off reward; battles cost nothing.
/// </summary>
public class WarfrontCampaignTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Warfront;
    protected override int MinimumCharacterLevel => 50;

    protected override string DisplayName => "Campaign";

    // MaxBattles real battles (32 s seen, 20 rounds at most), the map reopened before each.
    internal override float? MaxRuntimeSeconds => 1200f;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);
    private static readonly TimeSpan RetryAfter = TimeSpan.FromHours(24);
    private const double RetryPowerGain = 1.05;
    private const int MaxBattles = 10;

    // A run that ends on MaxBattles still had fights to try (12 of 16 accounts on 03/10): back soon,
    // with the other tasks in between.
    private static readonly TimeSpan MoreBattlesDelay = TimeSpan.FromMinutes(30);

    private const int MaxBattlePolls = 90;
    private const float BattlePollSeconds = 2f;

    private MelonPreferences_Entry<string> _defeats;

    private record Defeat(int Mission, int Mode, double Power, DateTime Time);

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        _defeats ??= category.CreateEntry("defeats", "", "Defeats",
            "(auto-managed, don't edit) - mission:difficulty@battle power@time of the defeats waiting for a retry");
    }

    public override IEnumerator Execute()
    {
        yield return ApplySquad();

        var defeats = ParseDefeats(_defeats.Value);
        var tried = new HashSet<(int, int)>();
        var stopped = false;
        for (var attempt = 0; attempt < MaxBattles; attempt++)
        {
            var power = WarfrontCampaign.BattlePower;
            var unlocked = WarfrontCampaign.Unlocked();
            defeats.RemoveAll(d => !unlocked.Any(b => Same(b, d))); // won since

            var waiting = defeats.Where(d => power < d.Power * RetryPowerGain && DateTime.Now < d.Time + RetryAfter).ToList();
            var bar = waiting.Select(d => WarfrontCampaign.VsEnemy(d.Power, d.Mission, d.Mode)).DefaultIfEmpty(0).Max();
            var candidates = unlocked
                .Where(b => b.Required <= power && WarfrontCampaign.VsEnemy(power, b.Mission, b.Mode) > bar &&
                            !waiting.Any(d => Same(b, d)) && !tried.Contains((b.Mission, b.Mode)))
                .OrderBy(b => b.Required).ToList();

            if (attempt == 0)
                Debug($"[INFO] Campaign: stars {WarfrontCampaign.Stars}, battle power {power:0}, unlocked " +
                      string.Join(", ", unlocked.OrderBy(b => b.Required).Take(5).Select(b => $"{b} {b.Required:0}")) +
                      $"; {waiting.Count} defeat(s) waiting (up to {bar:0.00}x the enemy), {candidates.Count} to try.");

            var next = candidates.FirstOrDefault();
            if (next == null)
            {
                stopped = true;
                break;
            }
            tried.Add((next.Mission, next.Mode));

            var stars = WarfrontCampaign.Stars;
            bool? won = null;
            yield return Fight(next, result => won = result);
            if (won == null) continue; // couldn't start it: already logged

            Debug($"[INFO] Campaign mission {next}: required {next.Required:0}, power {power:0}, ratio {power / next.Required:0.00} " +
                  $"({WarfrontCampaign.VsEnemy(power, next.Mission, next.Mode):0.00}x the enemy) " +
                  $"-> {(won.Value ? "won" : "lost")}, stars {stars} -> {WarfrontCampaign.Stars}.");
            if (won.Value) continue;

            defeats.RemoveAll(d => Same(next, d));
            defeats.Add(new Defeat(next.Mission, next.Mode, power, DateTime.Now));
            stopped = true;
            break;
        }

        _defeats.Value = string.Join(";", defeats.Select(d =>
            $"{d.Mission}:{d.Mode}@{d.Power.ToString("0", CultureInfo.InvariantCulture)}@{d.Time:s}"));
        NextRunTime = DateTime.Now + (stopped ? RecheckDelay : MoreBattlesDelay);
    }

    private static bool Same(CampaignBattle b, Defeat d) => b.Mission == d.Mission && b.Mode == d.Mode;

    /// <summary>True won, false lost, null when the fight didn't start (logged).</summary>
    private IEnumerator Fight(CampaignBattle battle, Action<bool?> result)
    {
        yield return WorldMap.Open;
        yield return WorldMap.OpenWarfrontCampaignTab;
        yield return WarfrontCampaign.OpenPreview(battle.Mission);

        var fight = WarfrontCampaign.FightButton(battle.Mode);
        if (fight == null || !fight.IsClickable())
        {
            Debug($"[FAILED] Campaign mission {battle}: preview open={WarfrontCampaign.IsPreviewVisible}, " +
                  "fight button missing or not clickable.");
            yield return Watchdog.ForceClearAll();
            yield break;
        }

        yield return fight.Click();
        yield return Poll.Until(() => WFBattle.IsVisible || WFBattleResult.IsDecided);
        if (!WFBattle.IsVisible && !WFBattleResult.IsDecided)
        {
            Debug($"[FAILED] Campaign mission {battle}: no battle after the click. Screens: {Watchdog.DumpActiveScreens()}");
            yield return Watchdog.ForceClearAll();
            yield break;
        }

        yield return Poll.Until(() => WFBattleResult.IsDecided, MaxBattlePolls, BattlePollSeconds);
        if (!WFBattleResult.IsDecided)
        {
            // Counted as a defeat, so the run stops here and doesn't open the map over a live battle.
            Logger.Warning($"[WarfrontCampaignTask] Mission {battle} didn't resolve within the wait bound.");
            result(false);
            yield break;
        }

        var won = WFBattleResult.IsWon;
        yield return WFBattleResult.Close;
        // Closes whatever a win leaves open (rewards), and the map: the next fight reopens it.
        yield return Watchdog.ForceClearAll();
        result(won);
    }

    /// <summary>
    ///     Fields the strongest squad (WarfrontCampaign.Strongest) and puts every free hero in a crew -
    ///     the Arena fights with the same squad (user decision, 03/10). Read from the game's data each
    ///     run, so the screen opens only when something is missing. When the machines change, they all
    ///     come out and go back in the strongest order, spot 0 first.
    /// </summary>
    private IEnumerator ApplySquad()
    {
        var squad = WarfrontCampaign.Squad();
        var strongest = WarfrontCampaign.Strongest(WarfrontCampaign.Machines());
        var (heroes, crewed, perMachine) = WarfrontCampaign.Crews();
        var sameMachines = WarfrontCampaign.SameSquad(squad, strongest);
        var freeHeroes = Math.Min(heroes - crewed, strongest.Count * perMachine - crewed);
        Debug($"[INFO] Campaign squad: {string.Join(", ", squad)}; strongest: " +
              $"{(sameMachines ? "the same" : string.Join(", ", strongest))}; heroes in crews {crewed} of {heroes}, " +
              $"{perMachine} crew slots per machine.");
        if (sameMachines && freeHeroes <= 0) yield break;

        var before = WarfrontCampaign.BattlePower;
        yield return WorldMap.Open;
        yield return WorldMap.OpenWarfrontCampaignTab;
        yield return WarfrontCampaign.OpenSquadScreen();
        if (!WarfrontCampaign.IsSquadScreenVisible)
        {
            Debug($"[FAILED] Campaign squad: screen not open. Screens: {Watchdog.DumpActiveScreens()}");
            yield return Watchdog.ForceClearAll();
            yield break;
        }

        if (!sameMachines)
        {
            foreach (var code in WarfrontCampaign.ScreenSpots().Where(s => s.warMachine != null).Select(s => s.warMachine.code).ToList())
                yield return WarfrontCampaign.DeckCard(code)?.Click();
            foreach (var machine in strongest)
                yield return WarfrontCampaign.DeckCard(machine.Code)?.Click();

            var placed = WarfrontCampaign.ScreenSpots().Select(s => s.warMachine?.code ?? "-").ToList();
            Debug($"[INFO] Campaign squad: spots now {string.Join(", ", WarfrontCampaign.ScreenSpots().Select(s => $"{s.spotIndex}:{s.warMachine?.name ?? "-"}[{WarfrontCampaign.CrewCount(s)}]"))}.");
            if (!placed.Take(strongest.Count).SequenceEqual(strongest.Select(m => m.Code)))
            {
                // Nothing is saved: closing drops the draft.
                Debug("[FAILED] Campaign squad: the spots don't match the strongest squad - not saved.");
                yield return Watchdog.ForceClearAll();
                yield break;
            }
        }

        // Free heroes go one at a time to the machine with the smallest crew, front first: the first
        // hero multiplies a machine's power about 13x, the second takes it to about 20x (Steam-0 03/10).
        var spots = WarfrontCampaign.ScreenSpots().Where(s => s.warMachine != null).ToList();
        var crews = spots.Select(WarfrontCampaign.CrewCount).ToArray();
        var adds = new int[spots.Count];
        for (var free = heroes - crews.Sum(); free > 0; free--)
        {
            var open = Enumerable.Range(0, spots.Count).Where(i => crews[i] + adds[i] < perMachine).ToList();
            if (open.Count == 0) break;
            adds[open.OrderBy(i => crews[i] + adds[i]).First()]++;
        }

        for (var i = 0; i < spots.Count; i++)
        {
            if (adds[i] == 0) continue;
            var edit = new GameButton(transform: (spots[i].editCrewButton.gameObject.activeInHierarchy
                ? spots[i].editCrewButton : spots[i].addCrewButton).transform);
            yield return edit.Click();
            yield return Poll.Until(() => WarfrontCampaign.IsCrewPopupVisible);
            foreach (var hero in WarfrontCampaign.FreeHeroCards().Take(adds[i]))
                yield return hero.Click();
            yield return new GameButton(Paths.SelectWarMachineHeroesLoc.SaveBtn).Click();
            yield return Poll.Until(() => !WarfrontCampaign.IsCrewPopupVisible);
            if (WarfrontCampaign.IsCrewPopupVisible) yield return new GameButton(Paths.SelectWarMachineHeroesLoc.CloseBtn).Click();
        }

        Debug($"[INFO] Campaign squad: spots before saving {string.Join(", ", WarfrontCampaign.ScreenSpots().Select(s => $"{s.spotIndex}:{s.warMachine?.name ?? "-"}[{WarfrontCampaign.CrewCount(s)}]"))}.");
        var save = new GameButton(Paths.SelectWarMachinesLoc.SaveBtn);
        yield return save.Click();
        yield return Poll.Until(() => !save.IsClickable() && WarfrontCampaign.BattlePower != before, 20);
        yield return Watchdog.ForceClearAll();

        var after = WarfrontCampaign.BattlePower;
        var (_, crewedAfter, _) = WarfrontCampaign.Crews();
        Debug($"[INFO] Campaign squad set: {string.Join(", ", WarfrontCampaign.Squad())}; heroes in crews {crewed} -> {crewedAfter}; " +
              $"battle power {before:0} -> {after:0}.");
        if (after < before) Debug($"[FAILED] Campaign squad: battle power dropped, {before:0} -> {after:0}.");
    }

    private static List<Defeat> ParseDefeats(string value)
    {
        var defeats = new List<Defeat>();
        foreach (var entry in value.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split('@');
            var key = parts[0].Split(':');
            if (parts.Length == 3 && key.Length == 2 &&
                int.TryParse(key[0], out var mission) && int.TryParse(key[1], out var mode) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var power) &&
                DateTime.TryParse(parts[2], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                defeats.Add(new Defeat(mission, mode, power, time));
        }

        return defeats;
    }
}
