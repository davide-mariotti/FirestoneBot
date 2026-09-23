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

    // Per the user (2026-09-23): a priority-name match always wins and stops the scan immediately
    // (no more comparing across trees) - matches the wiki's confirmed node names in the Firestone
    // Research trees ("Battle Cry"/"Librarian" don't appear in these trees at all, only in Personal
    // Tree/Talent Tree - see TreeOfLife.PriorityUpgrades and TalentsTask instead).
    private static readonly string[] PriorityTerms = { "Raining Gold", "Firestone Finder", "Firestone Effect" };

    private static bool IsPriority(string name) =>
        PriorityTerms.Any(t => name.Contains(t, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Picks the next talent to research. A priority-name match (see PriorityTerms) always wins
    ///     and stops the scan the instant one is found - no more comparing across trees once one
    ///     turns up. Only when no priority candidate exists anywhere reachable does the FIRST
    ///     unlocked, not-yet-maxed candidate encountered win (per the user, 2026-09-23: stop
    ///     comparing by time-to-complete across all nodes - that meant opening/closing a preview
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

            int? bestIndex = null;
            int? bestTreeOffset = null;
            var foundPriority = false;

            var treeOffset = 0;
            while (treeOffset < TreeCount && !foundPriority)
            {
                for (var index = 1; index <= NodeCount; index++)
                {
                    yield return node.Select(index);

                    if (Preview.IsUnlocked && !Preview.IsMaxed)
                    {
                        if (IsPriority(Preview.Name))
                        {
                            bestIndex = index;
                            bestTreeOffset = treeOffset;
                            foundPriority = true;
                            yield return Preview.Close;
                            break;
                        }

                        // First non-priority candidate found, kept only as a fallback - scanning
                        // continues in case a priority match still turns up in a later tree.
                        if (bestIndex == null)
                        {
                            bestIndex = index;
                            bestTreeOffset = treeOffset;
                        }
                    }

                    yield return Preview.Close;
                }

                if (foundPriority) break;

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

            if (bestIndex == null) yield break;

            // The scan above ends on the tree it actually stopped at (a priority hit, a locked
            // tree, or the last reachable one) - step back from there to the tree with the picked
            // node. Works regardless of whether the tree carousel wraps around or clamps at the
            // ends, since we only ever move backward from a known position toward a lower one.
            var lastReachedTree = Math.Min(treeOffset, TreeCount - 1);
            for (var back = lastReachedTree; back > bestTreeOffset; back--)
                yield return node.PreviousTree;

            Debug($"[INFO] Selected talent #{bestIndex} on tree offset {bestTreeOffset} (priority={foundPriority}).");

            yield return node.Select(bestIndex.Value);
            if (Preview.IsUnlocked && !Preview.IsMaxed) yield return Preview.Start;
        }
    }
}
