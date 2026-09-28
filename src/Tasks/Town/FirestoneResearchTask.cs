using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Library.FirestoneResearch;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Library = Firebot.GameModel.Features.Town.Library.Library;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

public class FirestoneResearchTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private const int NodeCount = 16;
    private const int TreeCount = 3;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.FirestoneResearch;

    public override IEnumerator Execute()
    {
        // Fast path: the notification (when up) opens the Library directly. Safe no-op otherwise.
        yield return Notifications.FirestoneResearch;

        // Guaranteed path regardless of the notification - same reasoning as the previous tasks:
        // don't rely on the screen already being open.
        yield return TownScreen.Open;
        yield return TownScreen.OpenLibrary;
        yield return Library.OpenFirestoneResearchTab;

        var panel = new ResearchPanel();
        yield return panel.Claim();

        // Buy a new concurrent research slot whenever affordable - the button itself is disabled
        // (a safe no-op click) once there's nothing left to unlock or not enough meteorites.
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
    ///     Priority node names, BEST FIRST - the position in this array is the rank, so "Raining Gold"
    ///     always beats "Firestone Effect" no matter which one the node scan happens to reach first.
    ///     Was an unordered set until 2026-09-28, which meant the winner was whichever priority name
    ///     sat at the lower node index - exactly backwards from the F2P guide, which puts gold ahead
    ///     of everything. Names verified against the wiki mirror in docs/wiki ("Battle Cry"/"Librarian"
    ///     don't appear in these trees at all, only in Personal Tree/Talent Tree - see
    ///     TreeOfLife.PriorityUpgrades and TalentsTask instead).
    ///     "Trainer Skills" and "Expeditioner" are the only two time-reduction nodes that exist
    ///     anywhere in the Firestone Research trees (both in tree 1, column 8 - checked across every
    ///     tree page in docs/wiki). The guide's Steam source ranks time reductions ABOVE gold, but
    ///     that does not survive the tree's own numbers: Raining Gold is 20% MULTIPLICATIVE over 25
    ///     levels in column 4, while these two are 1% ADDITIVE (the wiki's asterisk) capped at 25% and
    ///     20% respectively, four columns deeper. So they rank below the gold/firestone group here,
    ///     not above it. The guide's intent is honoured where those reductions actually live: the
    ///     research and alchemy ones are Talent Tree nodes, and TalentBuildConfig already has
    ///     Librarian at 100 and Alchemy at 95, the top two of the whole build.
    /// </summary>
    private static readonly string[] PriorityTerms =
    {
        "Raining Gold", "Firestone Finder", "Firestone Effect", "Trainer Skills", "Expeditioner"
    };

    /// <summary>Index into PriorityTerms (lower = better), or int.MaxValue when it isn't a priority
    /// node at all.</summary>
    private static int PriorityRank(string name)
    {
        for (var i = 0; i < PriorityTerms.Length; i++)
            if (name.Contains(PriorityTerms[i], StringComparison.OrdinalIgnoreCase))
                return i;

        return int.MaxValue;
    }

    /// <summary>
    ///     Picks the next talent to research. A priority-name match (see PriorityTerms) always wins
    ///     and stops the scan the instant one is found - no more comparing across trees once one
    ///     turns up. Only when no priority candidate exists anywhere reachable does the FIRST
    ///     unlocked, not-yet-maxed, LEVEL-0 candidate encountered wins - not any not-yet-maxed
    ///     candidate regardless of level. This matters because of how columns unlock (per the wiki's
    ///     "Unlock Requirements" section on each tree page): column N+1 needs only a small TOTAL level
    ///     count (4-8) summed across column N and earlier, spread across however many nodes exist
    ///     there - not any one node maxed out. Picking "first available at any level" would grind the
    ///     very first node all the way to its own max (e.g. level 50) before ever touching a later
    ///     column, which is far slower to reach a priority node gated behind several columns (like
    ///     Raining Gold) than briefly touching every node in each column once (satisfying the next
    ///     column's unlock threshold quickly) and circling back for depth later - per the user,
    ///     2026-09-23, who caught this after the click-reduction change below. Falls back to "first
    ///     available at any level" (the old fallback) only once a full scan finds no level-0 candidate
    ///     left anywhere reachable. Preview.CurrentLevel is read from the same popup already open for
    ///     the unlock/maxed checks, so this costs no extra clicks over the priority-match logic below.
    ///     Stop comparing by time-to-complete across all nodes - that meant opening/closing a preview
    ///     popup for every one of up to 3 trees x 16 nodes on every single slot-fill, a real CPU/click
    ///     cost multiplied across 16 bot instances; a priority match is common enough that this
    ///     usually stops the scan within the first tree or two instead of exhausting all of them).
    ///     Re-scans each time a slot frees up - but only as far as trees actually unlocked in-game
    ///     go; trees unlock sequentially (confirmed live: the game blocks navigation to tree N+1 with
    ///     "complete tree N first" until tree N is done), so scanning stops at the first tree that
    ///     turns out to still be locked instead of wastefully re-scanning the same reachable tree(s)
    ///     again under each locked attempt.
    /// </summary>
    private IEnumerator RunSelection()
    {
        var node = new Node();

        while (ResearchPanel.HasEmptySlot)
        {
            // Starting a research can close the whole Library/FirestoneResearch screen (confirmed
            // live: right after Preview.Start, with a second empty slot still to fill, no tree was
            // visible any more and even Library's own closeButton had gone invisible - the screen
            // had left entirely). Re-open before every pick instead of assuming the screen stayed
            // open from the previous one; a safe no-op when it did.
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
            // bestPriorityRank == 0 means Raining Gold was found - nothing can outrank it, so the
            // whole scan stops there.
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

                            // Rank 0 (Raining Gold) is unbeatable - stop here instead of paying for
                            // the rest of the tree. Any other rank keeps scanning: breaking out on
                            // the first priority hit is what used to let a worse-ranked name win
                            // just by sitting at a lower node index.
                            if (rank == 0)
                            {
                                yield return Preview.Close;
                                break;
                            }
                        }

                        // First fresh (untouched) candidate - the cheapest way to satisfy the next
                        // column's unlock threshold. Recorded rather than taken immediately: a
                        // better-ranked priority further along this same tree has to be able to
                        // outrank it.
                        if (Preview.CurrentLevel == 0 && freshIndex == null)
                        {
                            freshIndex = index;
                            freshTreeOffset = treeOffset;
                        }

                        // First already-touched candidate found, kept only as a last-resort
                        // fallback in case no level-0 candidate exists anywhere reachable.
                        if (fallbackIndex == null && Preview.CurrentLevel > 0)
                        {
                            fallbackIndex = index;
                            fallbackTreeOffset = treeOffset;
                        }
                    }

                    yield return Preview.Close;
                }

                // Same as before: once this tree has produced something usable, don't scan further
                // trees for a marginally better candidate.
                if (bestPriorityIndex != null || freshIndex != null) break;

                if (treeOffset < TreeCount - 1)
                {
                    var beforeTree = node.CurrentTreeName;
                    yield return node.NextTree;

                    if (node.CurrentTreeName == beforeTree)
                    {
                        // Didn't actually move - the next tree is locked ("complete tree N
                        // first"). Dismiss just that validation toast (NOT Watchdog.ForceClearAll:
                        // its generic "menus/" sweep would also close the Library/FirestoneResearch
                        // screen itself, since that has its own visible closeButton too - which
                        // aborted the whole task before it could select anything, wasting the scan
                        // that just ran) and stop scanning further trees this pass instead of
                        // wastefully re-scanning this same tree TreeCount-1-treeOffset more times.
                        yield return new GameButton(Paths.MenusLoc.GenericMessageLoc.CloseBtn).Click();
                        break;
                    }
                }

                treeOffset++;
            }

            // Resolution order: best-ranked priority, then the first fresh (level-0) candidate, then
            // the first already-touched one - same three tiers as before, only the priority tier is
            // now ranked internally instead of "first one seen wins".
            var bestIndex = bestPriorityIndex ?? freshIndex ?? fallbackIndex;
            var bestTreeOffset = bestPriorityIndex != null ? bestPriorityTreeOffset
                : freshIndex != null ? freshTreeOffset
                : fallbackTreeOffset;

            if (bestIndex == null) yield break;

            // The scan above ends on the tree it actually stopped at (a priority/fresh hit, a locked
            // tree, or the last reachable one) - step back from there to the tree with the picked
            // node. Works regardless of whether the tree carousel wraps around or clamps at the
            // ends, since we only ever move backward from a known position toward a lower one.
            var lastReachedTree = Math.Min(treeOffset, TreeCount - 1);
            for (var back = lastReachedTree; back > bestTreeOffset; back--)
                yield return node.PreviousTree;

            Debug($"[INFO] Selected talent #{bestIndex} on tree offset {bestTreeOffset} " +
                  $"(priorityRank={(bestPriorityRank == int.MaxValue ? "none" : bestPriorityRank.ToString())}" +
                  $"{(bestPriorityIndex == null && freshIndex != null ? ", fresh" : "")}).");

            yield return node.Select(bestIndex.Value);
            if (Preview.IsUnlocked && !Preview.IsMaxed) yield return Preview.Start;
        }
    }
}
