using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Map.WarfrontCampaign;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Il2Cpp;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Spends blueprints in BlueprintPlanner's order: the campaign squad first, each role's main
///     attribute first (armor on the tank, damage on the others), then the machines out of the squad
///     for their milestones. Stops when the next pick costs more than the blueprints left, so they
///     wait for it. First run on Steam-28 (19/10): 78 levels, 8803 -> 33 blueprints, 244 s.
/// </summary>
public class WarMachineBlueprintsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    protected override int MinimumCharacterLevel => 50;

    // ~2-4 s a level: a backlog of thousands of blueprints is 50+ levels on the first run.
    internal override float? MaxRuntimeSeconds => 900f;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(12);

    private const int MaxUpgrades = 300;

    private static List<BlueprintOption> Options()
    {
        var squad = WarfrontCampaign.Squad().Select(m => m.Code).ToHashSet();
        return WarMachines.OpenBlueprintUpgrades().Select(u => new BlueprintOption(
            u.machineRef.code, u.machineRef.name, (int)u.machineRef.specialization, (int)u.code,
            u.level, u.upgradeCostSingle, squad.Contains(u.machineRef.code))).ToList();
    }

    public override IEnumerator Execute()
    {
        NextRunTime = DateTime.Now + RecheckDelay;

        var start = WarMachines.Blueprints;
        var next = BlueprintPlanner.Next(Options());
        if (next == null || next.Cost > start)
        {
            Debug($"[INFO] Blueprints {start:0}: nothing to buy (next {next?.Name} {next?.Attribute} at {next?.Cost:0}).");
            yield break;
        }

        yield return TownScreen.Open;
        yield return TownScreen.OpenWarMachines;

        string shown = null;
        var bought = new List<string>();
        var stop = "limit reached";
        for (var n = 0; n < MaxUpgrades; n++)
        {
            next = BlueprintPlanner.Next(Options());
            if (next == null) { stop = "squad at its caps"; break; }
            if (next.Cost > WarMachines.Blueprints) { stop = $"next costs {next.Cost:0}"; break; }

            if (shown != next.Code)
            {
                yield return WarMachines.SelectMachine(next.Code);
                yield return WarMachines.OpenBlueprintTab;
                shown = next.Code;
            }

            WMBlueprintUpgradeInteraction row = null;
            yield return Poll.Until(() => (row = WarMachines.BlueprintRow(next.Code, next.Attribute)) != null);
            var button = row?.upgradeButton == null ? null : new GameButton(transform: row.upgradeButton.transform);
            if (button == null || !button.IsClickable())
            {
                Debug($"[FAILED] {next.Name} attribute {next.Attribute} level {next.Level}: " +
                      (row == null ? "row not shown." : "button not clickable."));
                stop = "failed";
                break;
            }

            yield return button.Click();
            yield return Poll.Until(() => WarMachines.BlueprintRow(next.Code, next.Attribute)?.upgrade.level > next.Level);
            if (!(WarMachines.BlueprintRow(next.Code, next.Attribute)?.upgrade.level > next.Level))
            {
                Debug($"[FAILED] {next.Name} attribute {next.Attribute}: clicked, level stayed at {next.Level}.");
                stop = "failed";
                break;
            }

            bought.Add($"{next.Name} {(new[] { "dmg", "hp", "arm" })[next.Attribute]} {next.Level + 1}");
        }

        var milestones = string.Join(", ", WarfrontCampaign.Squad()
            .Select(m => GameInitialize.HandlerLoader.WarMachineHandler.GetWarMachineByCode(m.Code))
            .Select(m => $"{m.name} {m.blueprintsTotalLevels} ({m.milestoneAttributeType})"));
        Debug($"[INFO] Blueprints {start:0} -> {WarMachines.Blueprints:0}, {bought.Count} levels, stop: {stop}. " +
              $"{string.Join(", ", bought)}. Milestones: {milestones}.");

        yield return WarMachines.Close;
        yield return TownScreen.Close;
    }
}
