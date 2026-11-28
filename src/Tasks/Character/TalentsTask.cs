using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Character;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Firebot.TalentEngine;
using MelonLoader;

namespace Firebot.Tasks.Character;

/// <summary>
///     Spends talent points as TalentAllocator plans them: deeper first, with the one point each tier
///     needs in the tier before it, then by priority. Levels and locks come from the game's data behind
///     each node, re-read after every save; points already spent are never touched. Nothing is invested
///     when the nodes' levels don't add up to the points counter.
/// </summary>
public class TalentsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.TalentAvailable;

    // After a reset all ~200 points go in one run, a click a second plus a save per node.
    internal override float? MaxRuntimeSeconds => 900f;

    private MelonPreferences_Entry<string> _priorityOverrides;
    private MelonPreferences_Entry<bool> _planOnly;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_priorityOverrides != null) return;

        _priorityOverrides = category.CreateEntry(
            "priority_overrides",
            "",
            "Priority Overrides",
            "Overrides the default per-talent priority (higher = invested first, see " +
            "TalentBuildConfig.DefaultPriorities). Comma-separated 'Name:Priority' pairs, e.g. " +
            "'Fate:100,Alchemy:80'. Names must match a real talent name - unmatched or malformed " +
            "entries are logged and ignored. Anything not listed here keeps its default priority."
        );

        _planOnly = category.CreateEntry(
            "plan_only",
            false,
            "Plan Only",
            "Logs the tree and the plan without investing anything, to check them first. Default is false."
        );
    }

    public override IEnumerator Execute()
    {
        // Runs only on the TalentAvailable badge, never on a timer.
        NextRunTime = DateTime.MaxValue;

        // The badge can outlive the points (a start-up flash): the game holds the count. The game's
        // alone: with the tab closed the counter's fallback would read 0.
        if (GameState.Get("talent points", () => GameState.H.TalentsHandler.availablePoints) is 0)
        {
            Debug("[INFO] From the game: no talent point to spend - not opening.");
            yield break;
        }

        yield return Notifications.TalentAvailable;

        yield return CharacterScreen.Open();
        if (!CharacterScreen.IsOpen)
        {
            Debug("Character screen never opened this run - skipping, will retry next scheduled run.");
            yield break;
        }

        yield return CharacterScreen.OpenTalentsTab;

        var tree = TalentTreeData.Tree;
        var priorities = TalentBuildConfig.Resolve(_priorityOverrides.Value, m => Debug(m));
        var excluded = new HashSet<int>();
        var first = true;

        while (Talents.AvailablePoints > 0)
        {
            var available = Talents.AvailablePoints;
            var spent = Talents.SpentPoints;
            if (first) GameState.Compare("talent points", $"{available}/{spent}", Talents.Screen);
            // A save reaches the nodes' data a moment after the counter.
            yield return Poll.Until(() => Read(tree).Ranks?.Sum() == spent);

            var (ranks, locked, problem) = Read(tree);
            if (problem == null && ranks.Sum() != spent)
                problem = $"the nodes hold {ranks.Sum()} points but the counter says {spent} spent";
            if (problem != null)
            {
                Debug($"[FAILED] Talents: {problem} - nothing invested.");
                break;
            }

            var plan = TalentAllocator.Plan(tree, ranks, excluded, available, priorities, TalentBuildConfig.DefaultPriority);
            if (first)
            {
                first = false;
                Debug($"[INFO] Talents: {available} to spend, {spent} spent, {TalentAllocator.OpenTiers(tree, ranks)} tiers open. " +
                      $"Tree: {Describe(tree, ranks, locked)}");
                Debug($"[INFO] Plan: {string.Join(", ", plan.Select(s => $"'{tree.Nodes[s.NodeIndex].Name}'+{s.PointsToAdd}"))}");
                if (_planOnly.Value) break;
            }

            if (plan.Count == 0) break;

            var step = plan[0];
            var node = tree.Nodes[step.NodeIndex];
            if (locked.Contains(step.NodeIndex))
            {
                Debug($"[INFO] '{node.Name}' [{step.NodeIndex}] is locked in the game - excluded, replanning.");
                excluded.Add(step.NodeIndex);
                continue;
            }

            yield return Talents.OpenNode(step.NodeIndex);
            var invested = 0;
            while (invested < step.PointsToAdd && Talents.UpgradeButton.IsClickable())
            {
                yield return Talents.UpgradeButton.Click();
                invested++;
            }

            yield return Talents.ClosePreview;
            if (invested > 0) yield return Talents.Save;

            Debug($"[INFO] Invested {invested} in '{node.Name}' [{step.NodeIndex}] (wanted {step.PointsToAdd}).");
            if (invested == 0)
            {
                excluded.Add(step.NodeIndex);
                continue;
            }

            yield return Poll.Until(() => Talents.AvailablePoints < available);
            if (Talents.AvailablePoints >= available)
            {
                Debug($"[FAILED] Talents: still {Talents.AvailablePoints} points after saving - stopped.");
                break;
            }
        }

        yield return CharacterScreen.Close;
    }

    /// <summary>
    ///     Every node's level from the game's data, and which ones the game has locked. A problem when a
    ///     node is missing or its cap isn't TalentTreeData's: the tree changed, so nothing gets planned.
    /// </summary>
    private static (int[] Ranks, HashSet<int> Locked, string Problem) Read(TalentTreeDefinition tree)
    {
        var ranks = new int[tree.Nodes.Count];
        var locked = new HashSet<int>();
        for (var i = 0; i < tree.Nodes.Count; i++)
        {
            var data = Talents.NodeData(i);
            if (data == null) return (null, null, $"node [{i}] '{tree.Nodes[i].Name}' isn't on the screen");
            if (data.levelCap != tree.Nodes[i].MaxRank)
                return (null, null, $"node [{i}] '{tree.Nodes[i].Name}' caps at {data.levelCap} in the game ('{data.name}'), {tree.Nodes[i].MaxRank} here");

            ranks[i] = (int)Math.Round((double)data.level);
            if (!data.unlocked) locked.Add(i);
        }

        return (ranks, locked, null);
    }

    // Nodes with points or open, and where the game's locks disagree with the tier rule.
    private static string Describe(TalentTreeDefinition tree, int[] ranks, HashSet<int> locked)
    {
        var open = TalentAllocator.OpenTiers(tree, ranks);
        var parts = new List<string>();
        for (var i = 0; i < tree.Nodes.Count; i++)
        {
            var node = tree.Nodes[i];
            bool modelOpen = node.Tier < open, gameOpen = !locked.Contains(i);
            if (ranks[i] == 0 && !modelOpen && !gameOpen) continue;
            var flag = modelOpen == gameOpen ? "" : gameOpen ? " (open in game)" : " (locked in game)";
            parts.Add($"[{i}] {node.Name} {ranks[i]}/{node.MaxRank}{flag}");
        }

        return string.Join(", ", parts);
    }
}
