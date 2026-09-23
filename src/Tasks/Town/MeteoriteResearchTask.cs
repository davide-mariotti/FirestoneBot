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

    // Per the user (2026-09-23): same priority-name set as FirestoneResearchTask, verified against
    // the wiki's Meteorite Research tree node names (also has Firestone Finder/Effect entries).
    private static readonly string[] PriorityTerms = { "Raining Gold", "Firestone Finder", "Firestone Effect" };

    private static bool IsPriority(string name) =>
        PriorityTerms.Any(t => name.Contains(t, StringComparison.OrdinalIgnoreCase));

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

        int? bestIndex = null;
        int? bestTreeOffset = null;
        var foundPriority = false;

        var treeOffset = 0;
        while (treeOffset < TreeCount && !foundPriority)
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
                        if (IsPriority(MeteoriteResearchPreview.Name))
                        {
                            bestIndex = index;
                            bestTreeOffset = treeOffset;
                            foundPriority = true;
                            yield return MeteoriteResearchPreview.Close;
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
                }

                yield return MeteoriteResearchPreview.Close;
            }

            if (foundPriority) break;

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

        if (bestIndex == null) yield break;

        // The scan above ends on the tree it actually stopped at (a priority hit, a locked tree,
        // or the last reachable one) - step back from there to the tree with the picked node.
        // Works regardless of whether the tree carousel wraps around or clamps at the ends, since
        // we only ever move backward from a known position toward a lower one.
        var lastReachedTree = Math.Min(treeOffset, TreeCount - 1);
        for (var back = lastReachedTree; back > bestTreeOffset; back--)
            yield return node.PreviousTree;

        Debug($"[INFO] Selected meteorite research node #{bestIndex} on tree offset {bestTreeOffset} " +
              $"(priority={foundPriority}). Attempting - safe no-op if not yet affordable.");

        yield return node.Select(bestIndex.Value);
        yield return MeteoriteResearchPreview.Research;
        yield return MeteoriteResearchPreview.Close;
    }
}
