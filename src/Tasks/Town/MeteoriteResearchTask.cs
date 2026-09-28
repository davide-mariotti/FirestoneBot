using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Library.MeteoriteResearch;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;
using Library = Firebot.GameModel.Features.Town.Library.Library;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     The Library's OTHER research tab (Ricerca Meteoriti - talent bonuses across 5 trees of 13
///     nodes each), separate from FirestoneResearchTask's tech tree. Different theme on purpose:
///     Firestone Research runs continuously (always start the next talent the moment a slot frees
///     up), while Meteorite Research is gated by a currency (Meteorites, shared with gear tier
///     unlocks per the wiki) spent per node - there is nothing to do until enough has accumulated. A
///     priority-name match (see PriorityTerms) always wins and stops the scan immediately; otherwise
///     the first unlocked candidate found wins - per the user, 2026-09-23: switched away from
///     scanning every node for the globally cheapest one, to cut down the click/CPU cost of the scan
///     itself (see RunResearch).
///     Per the user (2026-09-23): never research below min_meteorite_reserve Meteorites, to keep a
///     margin for hero gear tier unlocks. Reads the balance from Paths.MenusLoc.LibraryLoc
///     .MeteoriteBalanceTxt - a currency counter at the Library screen's root level (sibling of
///     "submenus", so visible regardless of which tab is open), found via a fresh UnityPy dump of
///     the whole Library prefab - previously assumed unreadable (see the old recheck_interval_minutes
///     comment), but the user insisted it exists on-screen and was right.
///     Never automated before. (docs/path.firestone.html marks its
///     notification badge "Rimossa dal bot, feature mai raggiunta"). Paths sourced from a fresh
///     UnityPy scan of the live game assets, not just the docs (which didn't capture the preview
///     popup or the per-node cost display) - flagged for live verification throughout.
/// </summary>
public class MeteoriteResearchTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private const int TreeCount = 5;
    private const int NodeCount = 13; // research0..12 per tree

    private MelonPreferences_Entry<int> _recheckIntervalMinutes;
    private MelonPreferences_Entry<int> _minMeteoriteReserve;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_recheckIntervalMinutes != null) return;

        _recheckIntervalMinutes = category.CreateEntry(
            "recheck_interval_minutes",
            60,
            "Recheck Interval (minutes)",
            "How long to wait before checking again when the cheapest available research still " +
            "isn't affordable, or when the reserve threshold below is blocking research. Default: 60."
        );

        _minMeteoriteReserve = category.CreateEntry(
            "min_meteorite_reserve",
            3000,
            "Minimum Meteorite Reserve",
            "Never research below this Meteorite balance, so there's always a margin left for hero " +
            "gear tier unlocks (Meteorites are a currency shared between the two, per the wiki). " +
            "Default: 3000. Set to 0 to disable."
        );
    }

    // Abbreviated, not plain int - same reasoning as MeteoriteResearchPreview.Cost (this balance can
    // get large enough to show as "12.5K" etc., same UI convention as everywhere else in this game).
    private static double MeteoriteBalance =>
        new GameText(Paths.MenusLoc.LibraryLoc.MeteoriteBalanceTxt).GetParsedDoubleAbbreviated();

    public override IEnumerator Execute()
    {
        // Fast path: the notification (when up) opens the Library directly. Safe no-op otherwise.
        // Not independently verified - see Notifications.MeteoriteResearch.
        yield return Notifications.MeteoriteResearch;

        // Guaranteed path regardless of the notification - same reasoning as every other task.
        yield return TownScreen.Open;
        yield return TownScreen.OpenLibrary;
        yield return Library.OpenMeteoriteResearchTab;

        var minReserve = _minMeteoriteReserve?.Value ?? 3000;
        if (minReserve <= 0 || MeteoriteBalance >= minReserve)
            yield return RunResearch();
        else
            Debug($"[INFO] Meteorite balance below the {minReserve} reserve - skipping research this run.");

        NextRunTime = DateTime.Now + TimeSpan.FromMinutes(_recheckIntervalMinutes?.Value ?? 60);

        yield return Library.Close;
        yield return TownScreen.Close;
    }

    /// <summary>
    ///     Priority node names, BEST FIRST - same ranked treatment as FirestoneResearchTask (see its
    ///     own comment for why an unordered set was wrong). All three confirmed present in the
    ///     Meteorite Research trees via the wiki mirror in docs/wiki.
    ///     Deliberately does NOT carry FirestoneResearchTask's two extra time-reduction names: there
    ///     is no duration/cooldown node anywhere in the Meteorite Research trees (checked across
    ///     every tree page), so listing them here would be dead weight.
    /// </summary>
    private static readonly string[] PriorityTerms = { "Raining Gold", "Firestone Finder", "Firestone Effect" };

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
    ///     A priority-name match (see PriorityTerms) always wins and stops the scan the instant one
    ///     is found. Only when no priority candidate exists anywhere reachable does the FIRST
    ///     unlocked candidate with a real cost win (per the user, 2026-09-23: stop comparing cost
    ///     across all 5 trees x 13 nodes - up to 65 preview popups opened/closed just to find the
    ///     single cheapest one, every recheck_interval_minutes, across 16 bot instances).
    /// </summary>
    private IEnumerator RunResearch()
    {
        var node = new MeteoriteNode();

        int? bestPriorityIndex = null;
        int? bestPriorityTreeOffset = null;
        var bestPriorityRank = int.MaxValue;
        int? fallbackIndex = null;
        int? fallbackTreeOffset = null;

        var treeOffset = 0;
        // bestPriorityRank == 0 means Raining Gold was found - nothing can outrank it, so the whole
        // scan stops there.
        while (treeOffset < TreeCount && bestPriorityRank > 0)
        {
            for (var index = 0; index < NodeCount; index++)
            {
                yield return node.Select(index);

                if (MeteoriteResearchPreview.IsUnlocked)
                {
                    var cost = MeteoriteResearchPreview.Cost;

                    // cost <= 0 covers both "failed to parse" and "no cost shown" (e.g. an already
                    // maxed node) - either way, not a real candidate.
                    if (cost > 0)
                    {
                        var rank = PriorityRank(MeteoriteResearchPreview.Name);

                        if (rank < bestPriorityRank)
                        {
                            bestPriorityRank = rank;
                            bestPriorityIndex = index;
                            bestPriorityTreeOffset = treeOffset;

                            // Rank 0 (Raining Gold) is unbeatable - stop here. Any other rank keeps
                            // scanning: bailing out on the first priority hit is what used to let a
                            // worse-ranked name win just by sitting at a lower node index.
                            if (rank == 0)
                            {
                                yield return MeteoriteResearchPreview.Close;
                                break;
                            }
                        }

                        // First non-priority candidate found, kept only as a fallback - scanning
                        // continues in case a priority match still turns up in a later tree.
                        if (fallbackIndex == null && rank == int.MaxValue)
                        {
                            fallbackIndex = index;
                            fallbackTreeOffset = treeOffset;
                        }
                    }
                }

                yield return MeteoriteResearchPreview.Close;
            }

            // Same as before: a tree that produced a priority candidate ends the scan. The ranking
            // added 2026-09-28 applies WITHIN a tree (which is where the real problem was - two
            // priority names in the same tree, node index deciding the winner), deliberately not
            // across trees: chasing a possibly better-ranked name into the next tree would cost a
            // full extra 13-node preview sweep for a marginal gain.
            if (bestPriorityIndex != null) break;

            if (treeOffset < TreeCount - 1)
            {
                var beforeTree = node.CurrentTreeName;
                yield return node.NextTree;

                if (node.CurrentTreeName == beforeTree)
                {
                    // Didn't actually move - the next tree is locked ("complete tree N first").
                    // Same fix as FirestoneResearchTask: dismiss just that validation toast and
                    // stop scanning further trees instead of wastefully re-scanning this same
                    // tree under each locked attempt.
                    yield return new GameButton(Paths.MenusLoc.GenericMessageLoc.CloseBtn).Click();
                    break;
                }
            }

            treeOffset++;
        }

        // Best-ranked priority first, then the first affordable non-priority node found.
        var bestIndex = bestPriorityIndex ?? fallbackIndex;
        var bestTreeOffset = bestPriorityIndex != null ? bestPriorityTreeOffset : fallbackTreeOffset;

        if (bestIndex == null) yield break;

        // The scan above ends on the tree it actually stopped at (a priority hit, a locked tree,
        // or the last reachable one) - step back from there to the tree with the picked node.
        // Works regardless of whether the tree carousel wraps around or clamps at the ends, since
        // we only ever move backward from a known position toward a lower one.
        var lastReachedTree = Math.Min(treeOffset, TreeCount - 1);
        for (var back = lastReachedTree; back > bestTreeOffset; back--)
            yield return node.PreviousTree;

        Debug($"[INFO] Selected meteorite research node #{bestIndex} on tree offset {bestTreeOffset} " +
              $"(priorityRank={(bestPriorityRank == int.MaxValue ? "none" : bestPriorityRank.ToString())}). " +
              "Attempting - safe no-op if not yet affordable.");

        yield return node.Select(bestIndex.Value);
        yield return MeteoriteResearchPreview.Research;
        yield return MeteoriteResearchPreview.Close;
    }
}
