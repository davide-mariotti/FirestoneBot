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
using MelonLoader;
using Logger = Firebot.Core.Logger;
using CampaignBattle = Firebot.GameModel.Features.Map.WarfrontCampaign.WarfrontCampaign.Battle;

namespace Firebot.Tasks.Map;

/// <summary>
///     Pushes the Warfront campaign on (CAMPAIGN_PLAN.md): every mission/difficulty the squad's battle
///     power reaches, lowest requirement first, until the first defeat. The game's requirement only
///     allows a fight - Easy 32 was lost at 1.88x on Steam-0 (03/10) - so a defeat is retried only
///     at 5% more power or after 24 h, and meanwhile nothing with a lower margin is tried either.
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
        LogSquad();

        var defeats = ParseDefeats(_defeats.Value);
        var tried = new HashSet<(int, int)>();
        for (var attempt = 0; attempt < MaxBattles; attempt++)
        {
            var power = WarfrontCampaign.BattlePower;
            var unlocked = WarfrontCampaign.Unlocked();
            defeats.RemoveAll(d => !unlocked.Any(b => Same(b, d))); // won since

            var waiting = defeats.Where(d => power < d.Power * RetryPowerGain && DateTime.Now < d.Time + RetryAfter).ToList();
            var bar = waiting.Select(d => d.Power / WarfrontCampaign.Required(d.Mission, d.Mode)).DefaultIfEmpty(0).Max();
            var candidates = unlocked
                .Where(b => b.Required <= power && power / b.Required > bar &&
                            !waiting.Any(d => Same(b, d)) && !tried.Contains((b.Mission, b.Mode)))
                .OrderBy(b => b.Required).ToList();

            if (attempt == 0)
                Debug($"[INFO] Campaign: stars {WarfrontCampaign.Stars}, battle power {power:0}, unlocked " +
                      string.Join(", ", unlocked.OrderBy(b => b.Required).Take(5).Select(b => $"{b} {b.Required:0}")) +
                      $"; {waiting.Count} defeat(s) waiting (margin up to {bar:0.00}), {candidates.Count} to try.");

            var next = candidates.FirstOrDefault();
            if (next == null) break;
            tried.Add((next.Mission, next.Mode));

            var stars = WarfrontCampaign.Stars;
            bool? won = null;
            yield return Fight(next, result => won = result);
            if (won == null) continue; // couldn't start it: already logged

            Debug($"[INFO] Campaign mission {next}: required {next.Required:0}, power {power:0}, ratio {power / next.Required:0.00} " +
                  $"-> {(won.Value ? "won" : "lost")}, stars {stars} -> {WarfrontCampaign.Stars}.");
            if (won.Value) continue;

            defeats.RemoveAll(d => Same(next, d));
            defeats.Add(new Defeat(next.Mission, next.Mode, power, DateTime.Now));
            break;
        }

        _defeats.Value = string.Join(";", defeats.Select(d =>
            $"{d.Mission}:{d.Mode}@{d.Power.ToString("0", CultureInfo.InvariantCulture)}@{d.Time:s}"));
        NextRunTime = DateTime.Now + RecheckDelay;
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

    // ponytail: reports the strongest squad without applying it. Steam-0 already has it (03/10), so
    // the squad and crew clicks couldn't be seen live; the fleet's lines say which accounts need them.
    private void LogSquad()
    {
        var squad = WarfrontCampaign.Squad();
        var strongest = WarfrontCampaign.Strongest(WarfrontCampaign.Machines());
        var (heroes, crewed, slots) = WarfrontCampaign.Crews();
        Debug($"[INFO] Campaign squad: {string.Join(", ", squad)}; strongest: " +
              $"{(WarfrontCampaign.SameSquad(squad, strongest) ? "the same" : string.Join(", ", strongest))}; " +
              $"heroes in crews {crewed} of {heroes}, crew slots {slots}.");
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
