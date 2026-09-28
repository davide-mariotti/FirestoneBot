using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Library.FirestoneResearch;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Library = Firebot.GameModel.Features.Town.Library.Library;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>Keeps every Firestone Research slot busy, buying a new slot whenever it's affordable.</summary>
public class FirestoneResearchTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private const int NodeCount = 16;
    private const int TreeCount = 3;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.FirestoneResearch;

    public override IEnumerator Execute()
    {
        yield return Notifications.FirestoneResearch;

        yield return TownScreen.Open;
        yield return TownScreen.OpenLibrary;
        yield return Library.OpenFirestoneResearchTab;

        var panel = new ResearchPanel();
        yield return panel.Claim();

        // Disabled - a no-op - while no slot is left to buy or the meteorites don't cover it.
        yield return new GameButton(Paths.MenusLoc.LibraryLoc.ResearchPanelLoc.UnlockSlotBtn).Click();

        if (!ResearchPanel.HasEmptySlot)
        {
            Debug("[INFO] No empty slots. Scheduling next run.");
            NextRunTime = panel.NextRunTime();
            yield return Library.Close;
            yield return TownScreen.Close;
            yield break;
        }

        yield return RunSelection();
        NextRunTime = panel.NextRunTime();

        yield return Library.Close;
        yield return TownScreen.Close;
    }

    /// <summary>
    ///     Priority node names, best first: the position is the rank, so Raining Gold beats Firestone
    ///     Effect whichever the scan reaches first. Trainer Skills and Expeditioner are the only time
    ///     reductions in these trees. The F2P guide ranks time reductions above gold, but here they're
    ///     1% additive per level (capped at 25% and 20%, in column 8) against Raining Gold's 20%
    ///     multiplicative (column 4), so they come last. The guide's research and alchemy reductions
    ///     are Talent Tree nodes, already TalentBuildConfig's top two.
    /// </summary>
    private static readonly string[] PriorityTerms =
    {
        "Raining Gold", "Firestone Finder", "Firestone Effect", "Trainer Skills", "Expeditioner"
    };

    // Index into PriorityTerms (lower is better), int.MaxValue for a non-priority node.
    private static int PriorityRank(string name)
    {
        for (var i = 0; i < PriorityTerms.Length; i++)
            if (name.Contains(PriorityTerms[i], StringComparison.OrdinalIgnoreCase))
                return i;

        return int.MaxValue;
    }

    /// <summary>
    ///     Fills every empty slot. Per slot, the pick is the best-ranked priority node, else the first
    ///     untouched (level 0) node, else the first one already started. Untouched first because a
    ///     column unlocks on a small total level across the previous columns: touching each node once
    ///     opens the next column far sooner than maxing one node - and the priorities sit several
    ///     columns in. Trees unlock in order, so the scan stops at the first locked one, and after the
    ///     first tree that offered a priority or untouched node.
    /// </summary>
    private IEnumerator RunSelection()
    {
        var node = new Node();

        while (ResearchPanel.HasEmptySlot)
        {
            // Starting a research can close the whole screen, so it's reopened before every pick.
            yield return TownScreen.Open;
            yield return TownScreen.OpenLibrary;
            yield return Library.OpenFirestoneResearchTab;

            int? bestPriorityIndex = null;
            int? bestPriorityTreeOffset = null;
            var bestPriorityRank = int.MaxValue;
            int? freshIndex = null;
            int? freshTreeOffset = null;
            int? fallbackIndex = null;
            int? fallbackTreeOffset = null;

            var treeOffset = 0;
            while (treeOffset < TreeCount && bestPriorityRank > 0)
            {
                for (var index = 1; index <= NodeCount; index++)
                {
                    yield return node.Select(index);

                    if (Preview.IsUnlocked && !Preview.IsMaxed)
                    {
                        var rank = PriorityRank(Preview.Name);

                        if (rank < bestPriorityRank)
                        {
                            bestPriorityRank = rank;
                            bestPriorityIndex = index;
                            bestPriorityTreeOffset = treeOffset;

                            // Nothing outranks rank 0 - stop instead of scanning the rest of the tree.
                            if (rank == 0)
                            {
                                yield return Preview.Close;
                                break;
                            }
                        }

                        if (Preview.CurrentLevel == 0 && freshIndex == null)
                        {
                            freshIndex = index;
                            freshTreeOffset = treeOffset;
                        }

                        if (fallbackIndex == null && Preview.CurrentLevel > 0)
                        {
                            fallbackIndex = index;
                            fallbackTreeOffset = treeOffset;
                        }
                    }

                    yield return Preview.Close;
                }

                if (bestPriorityIndex != null || freshIndex != null) break;

                if (treeOffset < TreeCount - 1)
                {
                    var beforeTree = node.CurrentTreeName;
                    yield return node.NextTree;

                    if (node.CurrentTreeName == beforeTree)
                    {
                        // Refused: the next tree is locked. Close just the toast - Watchdog's menus/
                        // sweep would close the Library itself too.
                        yield return new GameButton(Paths.MenusLoc.GenericMessageLoc.CloseBtn).Click();
                        break;
                    }
                }

                treeOffset++;
            }

            var bestIndex = bestPriorityIndex ?? freshIndex ?? fallbackIndex;
            var bestTreeOffset = bestPriorityIndex != null ? bestPriorityTreeOffset
                : freshIndex != null ? freshTreeOffset
                : fallbackTreeOffset;

            if (bestIndex == null) yield break;

            // Walk back from wherever the scan stopped to the picked node's tree.
            var lastReachedTree = Math.Min(treeOffset, TreeCount - 1);
            for (var back = lastReachedTree; back > bestTreeOffset; back--)
                yield return node.PreviousTree;

            Debug($"[INFO] Selected research #{bestIndex} on tree offset {bestTreeOffset} " +
                  $"(priorityRank={(bestPriorityRank == int.MaxValue ? "none" : bestPriorityRank.ToString())}" +
                  $"{(bestPriorityIndex == null && freshIndex != null ? ", fresh" : "")}).");

            yield return node.Select(bestIndex.Value);
            if (Preview.IsUnlocked && !Preview.IsMaxed) yield return Preview.Start;
        }
    }
}
