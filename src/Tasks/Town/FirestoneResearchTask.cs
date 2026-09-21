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
    ///     Picks the next talent to research purely by shortest time-to-complete, across all 3 trees.
    ///     Rushing one talent to a high level over many days while the rest of the tree sits at 0 is a
    ///     worse use of time than spreading the same total time across many cheaper talents - so the
    ///     fastest currently-researchable option wins, EXCEPT "Raining Gold" always wins over
    ///     everything else when it's an unlocked option (an external tips guide the user found rates
    ///     it top priority - "Raining Gold ★★★★★" - and the user asked to prioritize it here; among
    ///     multiple unlocked Raining Gold instances across the 3 trees, still picks the fastest one).
    ///     Re-scans and compares every node in every tree each time a slot frees up - but only as
    ///     far as trees actually unlocked in-game go; trees unlock sequentially (confirmed live:
    ///     the game blocks navigation to tree N+1 with "complete tree N first" until tree N is
    ///     done), so scanning stops at the first tree that turns out to still be locked instead of
    ///     wastefully re-scanning the same reachable tree(s) again under each locked attempt.
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
            var bestTime = TimeSpan.MaxValue;
            var bestIsGold = false;

            var treeOffset = 0;
            for (; treeOffset < TreeCount; treeOffset++)
            {
                for (var index = 1; index <= NodeCount; index++)
                {
                    yield return node.Select(index);

                    if (Preview.IsUnlocked && !Preview.IsMaxed)
                    {
                        var isGold = Preview.Name.Contains("Raining Gold", StringComparison.OrdinalIgnoreCase);
                        var time = Preview.TimeRequired;

                        // A gold candidate always beats a non-gold one, regardless of time; among two
                        // candidates of the same gold-ness, the faster one wins.
                        var better = isGold != bestIsGold ? isGold : time < bestTime;

                        if (better)
                        {
                            bestTime = time;
                            bestIndex = index;
                            bestTreeOffset = treeOffset;
                            bestIsGold = isGold;
                        }
                    }

                    yield return Preview.Close;
                }

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
            }

            if (bestIndex == null) yield break;

            // The scan above ends on the last tree it actually reached (TreeCount - 1 normally,
            // or earlier if a later tree turned out to be locked) - step back from there to the
            // tree with the cheapest pick. Works regardless of whether the tree carousel wraps
            // around or clamps at the ends, since we only ever move backward from a known
            // position toward a lower one.
            var lastReachedTree = Math.Min(treeOffset, TreeCount - 1);
            for (var back = lastReachedTree; back > bestTreeOffset; back--)
                yield return node.PreviousTree;

            Debug($"[INFO] Selected talent #{bestIndex} on tree offset {bestTreeOffset} ({bestTime} to complete).");

            yield return node.Select(bestIndex.Value);
            if (Preview.IsUnlocked && !Preview.IsMaxed) yield return Preview.Start;
        }
    }
}
