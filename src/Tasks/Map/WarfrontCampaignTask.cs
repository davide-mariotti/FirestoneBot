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
using Il2Cpp;
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
    ///     Fields the strongest squad (WarfrontCampaign.Strongest) and its crews (CrewPlanner: every free
    ///     hero in, and heroes on the machine of their own specialization when there are any) - the Arena
    ///     fights with the same squad (user decisions, 03/10). Decided from the game's data each run, so
    ///     the screen opens only when something changes. When the machines change they all come out,
    ///     which empties their crews, and go back in the strongest order, spot 0 first.
    /// </summary>
    private IEnumerator ApplySquad()
    {
        var squad = WarfrontCampaign.Squad();
        var strongest = WarfrontCampaign.Strongest(WarfrontCampaign.Machines());
        var sameMachines = WarfrontCampaign.SameSquad(squad, strongest);
        var heroes = WarfrontCampaign.Heroes();
        var roles = heroes.ToDictionary(h => h.Code, h => h.Role);
        var perMachine = WarfrontCampaign.CrewSlotsPerMachine;
        var saved = WarfrontCampaign.SquadCrews();
        var savedSpots = saved.Select(s => new CrewSpot((int)s.Machine.Role, s.Crew)).ToList();
        var plan = CrewPlanner.Plan(savedSpots, roles, perMachine);
        var moves = sameMachines ? CrewPlanner.Moves(savedSpots, plan) : -1;
        Debug($"[INFO] Campaign squad: {Describe(saved, heroes)}; strongest: " +
              $"{(sameMachines ? "the same" : string.Join(", ", strongest))}; {heroes.Count} heroes, {perMachine} crew slots " +
              $"per machine, {(sameMachines ? $"{moves} crew move(s)" : "machines to change")}.");
        if (moves == 0) yield break;

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
            if (!placed.Take(strongest.Count).SequenceEqual(strongest.Select(m => m.Code)))
            {
                // Nothing is saved: closing drops the draft.
                Debug($"[FAILED] Campaign squad: the spots ({string.Join(", ", placed)}) don't match the strongest squad - not saved.");
                yield return Watchdog.ForceClearAll();
                yield break;
            }
        }

        var spots = WarfrontCampaign.ScreenSpots().Where(s => s.warMachine != null).ToList();
        if (!sameMachines)
            plan = CrewPlanner.Plan(spots.Select(s => new CrewSpot((int)s.warMachine.specialization, WarfrontCampaign.ScreenCrew(s))).ToList(),
                roles, perMachine);
        yield return SetCrews(spots, plan);
        if (!CrewsAre(spots, plan))
        {
            Debug($"[FAILED] Campaign squad: crews {Describe(spots, heroes)} instead of the plan - not saved.");
            yield return Watchdog.ForceClearAll();
            yield break;
        }

        var save = new GameButton(Paths.SelectWarMachinesLoc.SaveBtn);
        yield return save.Click();
        yield return Poll.Until(() => !save.IsClickable() && WarfrontCampaign.BattlePower != before, 20);
        var after = WarfrontCampaign.BattlePower;
        Debug($"[INFO] Campaign squad set: {Describe(spots, heroes)}; battle power {before:0} -> {after:0}.");

        if (after < before && sameMachines)
        {
            // Only heroes moved: put the saved crews back.
            var back = saved.Select(s => s.Crew).ToList();
            yield return SetCrews(spots, back);
            yield return save.Click();
            yield return Poll.Until(() => !save.IsClickable() && WarfrontCampaign.BattlePower != after, 20);
            Debug($"[FAILED] Campaign squad: battle power dropped, {before:0} -> {after:0}; crews put back " +
                  $"({(CrewsAre(spots, back) ? "done" : "not matching")}), battle power {WarfrontCampaign.BattlePower:0}.");
        }
        else if (after < before) Debug($"[FAILED] Campaign squad: battle power dropped, {before:0} -> {after:0}.");

        yield return Watchdog.ForceClearAll();
    }

    // A hero is listed only in its own crew's popup and among the free ones: everyone leaving a crew
    // goes out first, then the newcomers go in.
    private IEnumerator SetCrews(List<WarMachineFormationSettingSpot> spots, List<List<int>> target)
    {
        for (var i = 0; i < spots.Count; i++)
        {
            var leaving = WarfrontCampaign.ScreenCrew(spots[i]).Except(target[i]).ToList();
            if (leaving.Count > 0) yield return ToggleCrew(spots[i], leaving);
        }

        for (var i = 0; i < spots.Count; i++)
        {
            var joining = target[i].Except(WarfrontCampaign.ScreenCrew(spots[i])).ToList();
            if (joining.Count > 0) yield return ToggleCrew(spots[i], joining);
        }
    }

    /// <summary>Opens a spot's crew, clicks each hero's card (in or out of the crew), saves the crew.</summary>
    private IEnumerator ToggleCrew(WarMachineFormationSettingSpot spot, List<int> heroes)
    {
        var edit = spot.editCrewButton.gameObject.activeInHierarchy ? spot.editCrewButton : spot.addCrewButton;
        yield return new GameButton(transform: edit.transform).Click();
        yield return Poll.Until(() => WarfrontCampaign.IsCrewPopupVisible);
        foreach (var code in heroes)
        {
            var card = WarfrontCampaign.HeroCard(code);
            if (card?.button == null)
            {
                Debug($"[FAILED] Campaign squad: hero {code} isn't in {spot.warMachine?.name}'s crew list.");
                continue;
            }

            yield return new GameButton(transform: card.button.transform).Click();
        }

        yield return new GameButton(Paths.SelectWarMachineHeroesLoc.SaveBtn).Click();
        yield return Poll.Until(() => !WarfrontCampaign.IsCrewPopupVisible);
        if (WarfrontCampaign.IsCrewPopupVisible) yield return new GameButton(Paths.SelectWarMachineHeroesLoc.CloseBtn).Click();
    }

    private static bool CrewsAre(List<WarMachineFormationSettingSpot> spots, List<List<int>> target) =>
        spots.Select((s, i) => WarfrontCampaign.ScreenCrew(s).ToHashSet().SetEquals(target[i])).All(same => same);

    // "Goliath Tank 10285 [Boris T, Leo T]": each hero's specialization initial.
    private static string Describe(IEnumerable<(WarfrontCampaign.Machine Machine, List<int> Crew)> crews, List<WarfrontCampaign.Hero> heroes)
    {
        string Name(int code) => heroes.FirstOrDefault(h => h.Code == code) is { } hero ? $"{hero.Name} {"DTH"[hero.Role]}" : code.ToString();
        return string.Join(", ", crews.Select(c => $"{c.Machine} [{string.Join(", ", c.Crew.Select(Name))}]"));
    }

    private static string Describe(List<WarMachineFormationSettingSpot> spots, List<WarfrontCampaign.Hero> heroes) =>
        Describe(spots.Select(s => (new WarfrontCampaign.Machine(s.warMachine.code, s.warMachine.name, s.warMachine.specialization,
            s.warMachine.powerNoCrew), WarfrontCampaign.ScreenCrew(s))), heroes);

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
