using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Character;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Firebot.TalentEngine;
using MelonLoader;

namespace Firebot.Tasks.Character;

/// <summary>
///     Spends talent points to reach as deep into the 46-tier tree as possible (TalentAllocator
///     decides), re-planning from the live state on every run and never touching points already spent.
///     The game's per-node prerequisites aren't mapped: a node that turns out locked is excluded and
///     the plan redone, which converges within 89 re-plans. Only open tiers are read, and nodes
///     recorded as maxed (known_maxed_nodes) are skipped, so a run only reads the tree's frontier.
/// </summary>
public class TalentsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.TalentAvailable;

    private MelonPreferences_Entry<string> _priorityOverrides;
    private MelonPreferences_Entry<string> _knownMaxedNodes;

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

        _knownMaxedNodes = category.CreateEntry(
            "known_maxed_nodes",
            "",
            "Known Maxed Nodes",
            "(auto-managed, don't edit) - comma-separated catalog indices already confirmed at max " +
            "rank, so future runs don't need to re-open them just to read their level."
        );
    }

    public override IEnumerator Execute()
    {
        // Runs only on the TalentAvailable badge, never on a timer.
        NextRunTime = DateTime.MaxValue;

        yield return Notifications.TalentAvailable;

        yield return CharacterScreen.Open();
        if (!CharacterScreen.IsOpen)
        {
            Debug("Character screen never opened this run - skipping, will retry next scheduled run.");
            yield break;
        }

        yield return CharacterScreen.OpenTalentsTab;

        var availablePoints = Talents.AvailablePoints;

        if (availablePoints > 0)
        {
            var totalSpent = Talents.TotalPointsAwarded - availablePoints;
            Debug($"Available points: {availablePoints}, total spent: {totalSpent}.");

            var tree = TalentTreeData.Tree;
            var priorities = TalentBuildConfig.Resolve(_priorityOverrides.Value);
            var knownMaxed = ParseIndexSet(_knownMaxedNodes.Value);
            var lockedNodes = new HashSet<int>();
            var ranks = new int[tree.Nodes.Count];

            for (var i = 0; i < tree.Nodes.Count; i++)
            {
                var node = tree.Nodes[i];
                if (tree.TierThresholds[node.Tier] > totalSpent) continue; // tier not open - rank is always 0

                if (knownMaxed.Contains(i))
                {
                    ranks[i] = node.MaxRank;
                    Debug($"  [{i}] '{node.Name}' tier={node.Tier} rank={ranks[i]}/{node.MaxRank} (cached maxed) priority={PriorityOf(priorities, node.Name)}");
                    continue;
                }

                yield return Talents.OpenNode(i);

                if (Talents.IsPreviewLocked)
                {
                    lockedNodes.Add(i);
                    Debug($"  [{i}] '{node.Name}' tier={node.Tier} LOCKED priority={PriorityOf(priorities, node.Name)}");
                }
                else
                {
                    ranks[i] = Talents.PreviewCurrentRank;
                    if (ranks[i] >= node.MaxRank) MarkMaxed(i, knownMaxed);
                    Debug($"  [{i}] '{node.Name}' tier={node.Tier} rank={ranks[i]}/{node.MaxRank} priority={PriorityOf(priorities, node.Name)}");
                }

                yield return Talents.ClosePreview;
            }

            // Each pass re-plans from the corrected ranks and locks, so a misread doesn't leave points unspent.
            var progressMade = true;
            while (availablePoints > 0 && progressMade)
            {
                var plan = TalentAllocator.Plan(tree, ranks, lockedNodes, availablePoints, priorities,
                    TalentBuildConfig.DefaultPriority);
                if (plan.Count == 0)
                {
                    Debug("Plan() returned no steps - nothing more can be invested this run.");
                    break;
                }

                Debug($"Plan: {string.Join(", ", plan.Select(s => $"'{tree.Nodes[s.NodeIndex].Name}'+{s.PointsToAdd}"))}");

                progressMade = false;

                foreach (var step in plan)
                {
                    if (availablePoints <= 0) break;

                    var node = tree.Nodes[step.NodeIndex];
                    yield return Talents.OpenNode(step.NodeIndex);

                    if (Talents.IsPreviewLocked)
                    {
                        // Either its tier just opened within this plan, or it's one of the tree's
                        // unmapped per-node prerequisites: exclude it and re-plan.
                        Debug($"  '{node.Name}' unexpectedly locked at investment time - excluding and replanning.");
                        lockedNodes.Add(step.NodeIndex);
                        yield return Talents.ClosePreview;
                        progressMade = true;
                        continue;
                    }

                    var invested = 0;
                    for (var i = 0; i < step.PointsToAdd && availablePoints > 0 &&
                                    Talents.UpgradeButton.IsClickable(); i++)
                    {
                        yield return Talents.UpgradeButton.Click();
                        availablePoints--;
                        invested++;
                    }

                    Debug($"  Invested {invested} in '{node.Name}' (wanted {step.PointsToAdd}).");

                    yield return Talents.ClosePreview;
                    if (invested > 0) yield return Talents.Save;

                    // The button going unclickable early, with points left, means the node is really
                    // capped - trust the game over the earlier read.
                    var hitRealCap = invested < step.PointsToAdd && availablePoints > 0;
                    ranks[step.NodeIndex] = hitRealCap ? node.MaxRank : ranks[step.NodeIndex] + invested;
                    if (hitRealCap || ranks[step.NodeIndex] >= node.MaxRank) MarkMaxed(step.NodeIndex, knownMaxed);

                    if (invested > 0 || hitRealCap) progressMade = true;
                }
            }
        }

        yield return CharacterScreen.Close;
    }

    private static int PriorityOf(IReadOnlyDictionary<string, int> priorities, string name) =>
        priorities.TryGetValue(name, out var p) ? p : TalentBuildConfig.DefaultPriority;

    private void MarkMaxed(int nodeIndex, HashSet<int> knownMaxed)
    {
        if (!knownMaxed.Add(nodeIndex)) return;
        _knownMaxedNodes.Value = string.Join(",", knownMaxed.OrderBy(i => i));
    }

    private static HashSet<int> ParseIndexSet(string csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new HashSet<int>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var v) ? v : -1)
                .Where(v => v >= 0)
                .ToHashSet();
}
