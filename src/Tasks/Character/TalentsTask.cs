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
///     Spends talent points to maximize how deep the account can push into the 46-tier tree - see
///     TalentEngine.TalentAllocator for the actual decision algorithm and TalentTreeData for the tree's
///     real shape (both live-verified node-by-node on Steam-0, 2026-09-24). Per the user's explicit
///     design: the current state is only ever read as a snapshot - points already spent are never
///     touched or second-guessed, and every run recomputes the best allocation from scratch instead of
///     resuming a fixed sequence. This replaces the previous guide_start_index/CalibrateGuideStartIndex
///     approach entirely (a single hand-transcribed priority list covering only the tree's first ~448
///     of 2037 points, which the user never fully trusted enough to enable - see TESTING.md row 30).
///     The tree's real per-node "direct predecessor" prerequisite (the wiki: "also spend >=1 point in
///     the directly preceding talent") is deliberately NOT pre-mapped as static data - reverse-
///     engineering the exact pairs would need a fresh, zero-invested account to observe reliably, which
///     isn't available. Instead this task discovers a lock reactively (Paths.TalentPreviewLoc.LockedRoot,
///     the same signal the old task used) and replans around it: if a step the allocator expected to be
///     open comes back locked live, that node is excluded and Plan() is called again from the current
///     (already-applied) state. This converges within at most 89 replans (one new exclusion per replan,
///     89 nodes total) since the allocator always prefers other open candidates first.
///     Bootstrapping the account's current ranks only reads nodes whose tier is already open (a node in
///     a not-yet-reached tier is guaranteed rank 0 - it can never have received points) and skips
///     anything already recorded in known_maxed_nodes from a previous run, since a maxed node's rank can
///     never drop. This keeps the live-read cost bounded to the account's actual "frontier" instead of
///     rescanning the whole tree every run.
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
        // Notification-gated only, per the user (2026-09-24): this task must never fire off an idle
        // timer, only when the game's own TalentAvailable badge is actually showing - set unconditionally
        // up front (regardless of how this run ends) so IsReady's OR(notificationVisible, Now >=
        // NextRunTime) never takes the time branch for this task after its very first execution.
        NextRunTime = DateTime.MaxValue;

        // Fast path - safe no-op if not up.
        yield return Notifications.TalentAvailable;

        // Guaranteed path regardless of the notification - same reasoning as every other task.
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

            // Every pass re-plans from the just-corrected ranks/locks, so a bootstrap misread or a
            // node that turns out already maxed (real cap reached sooner than the read suggested)
            // is accounted for on the next iteration instead of silently leaving points unspent.
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
                        // Wasn't caught during the bootstrap read above - either this node's tier only
                        // opened via this same plan's own earlier steps, or it's genuinely the tree's
                        // unmapped per-node prerequisite. Either way, exclude it and let the next pass
                        // replan around it.
                        Debug($"  '{node.Name}' unexpectedly locked at investment time - excluding and replanning.");
                        lockedNodes.Add(step.NodeIndex);
                        yield return Talents.ClosePreview;
                        progressMade = true; // state changed (a new exclusion) - worth another pass
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

                    // A real cap hit sooner than the (possibly stale) bootstrap read expected shows up
                    // as IsClickable() going false before PointsToAdd was reached, with points still
                    // left to spend - trust the game over the read and snap straight to MaxRank so the
                    // next pass's candidate pool correctly excludes it instead of retrying forever.
                    var hitRealCap = invested < step.PointsToAdd && availablePoints > 0;
                    ranks[step.NodeIndex] = hitRealCap ? node.MaxRank : ranks[step.NodeIndex] + invested;
                    if (hitRealCap || ranks[step.NodeIndex] >= node.MaxRank) MarkMaxed(step.NodeIndex, knownMaxed);

                    // Either real spend happened, or we just learned this candidate's true (lower) cap -
                    // both change the candidate pool enough to be worth another Plan() pass.
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
