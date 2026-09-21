using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Spends Expedition Tokens on Personal Tree of Life upgrades (Guild -&gt; Tree of Life -&gt; Personal
///     tab) - never the Guild Tree, which affects the whole guild and requires leader/officer rank per
///     the wiki, out of scope per the user. Level 10 gate per the wiki's Tree of Life infobox.
///     This whole feature is new, requested by the user, who also gave the priority
///     order: "Raining Gold", "Firestone Finder" and "Firestone Effect" as a group take priority over
///     the other 17 upgrades - see TreeOfLife.PriorityUpgrades. Mirrors FirestoneResearchTask's
///     "Raining Gold always wins" selection pattern exactly, generalized from one priority name to a
///     small set: among affordable, not-yet-maxed candidates, a priority one always beats a
///     non-priority one regardless of level, and within the same tier the lowest-level (cheapest,
///     since the wiki confirms cost scales purely with an upgrade's own current level) wins.
///     Re-scans and re-picks after every single purchase, same reasoning as Research: spreads
///     investment across many upgrades instead of rushing one to a high level while the rest sit at 0.
///     Live-confirmed, 2026-09-18: clicking a node only opens a preview popup ("Magic spells / Level
///     0/5 / Buy upgrade 600") - it doesn't buy directly like the NodeLevelTxt comment originally
///     assumed (see TreeOfLife.ConfirmPurchase). Its buyUpgradeButton's own IsClickable() also doesn't
///     reliably predict real affordability - running out of Expedition Tokens pops a "CurrencyMissing"
///     warning instead of silently failing (see CurrencyMissingPopup, shared with War Machines' same
///     issue).
///     Bug found live, 2026-09-20 (user report: several non-priority perks stuck at level 0 with
///     currency clearly available for them): 1) the grid node's own IsClickable() does NOT tell you
///     anything about affordability or even "not maxed" - a node already at level 5 still reports
///     clickable=True (confirmed via a diagnostic dump of all 20 nodes), it only means "you can tap
///     it to open the preview popup". FindBestUpgrade now checks the node's own level against
///     MaxUpgradeLevel explicitly instead of trusting IsClickable() for that. 2) Far worse: the
///     priority-group override made the WHOLE run give up the instant the (always-tried-first)
///     priority group got too expensive to afford right now, via the unconditional `break` on
///     CurrencyMissingPopup - so the moment Raining Gold/Firestone Finder/Firestone Effect outgrew the
///     player's current Expedition Token balance, the other 17 - several sitting at level 0, i.e. the
///     cheapest possible purchases in the whole tree - never even got tried, even though they were
///     easily affordable. Fixed by tracking per-run "confirmed unaffordable right now" node indices
///     (populated from CurrencyMissingPopup) and excluding only those from the NEXT pick instead of
///     aborting the whole run - the priority group still gets first refusal whenever it's actually
///     affordable, but a temporarily-too-expensive priority node no longer blocks the other 17.
/// </summary>
public class TreeOfLifeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 10;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // Confirmed live, 2026-09-20 (see class doc comment) - every personal tree upgrade caps at this
    // level; the grid node's own IsClickable() doesn't reflect it, so FindBestUpgrade checks it itself.
    private const int MaxUpgradeLevel = 5;

    // Safety bound only - one node could in principle be bought many times in a row while cheapest,
    // so this isn't "one per upgrade", just a generous ceiling matching the pattern used everywhere
    // else in this codebase for a "keep going until nothing's left to do" loop.
    private const int MaxIterations = 200;

    public override IEnumerator Execute()
    {
        yield return TownGuild.Open;
        yield return TownGuild.OpenTreeOfLife;
        yield return TreeOfLife.OpenPersonalTab;

        var unaffordableThisRun = new HashSet<int>();

        for (var i = 0; i < MaxIterations; i++)
        {
            var best = FindBestUpgrade(unaffordableThisRun);
            if (best == null) break;

            yield return TreeOfLife.PersonalNode(best.Value).Click();
            yield return TreeOfLife.ConfirmPurchase();

            if (!CurrencyMissingPopup.IsShowing) continue;

            // This specific node is too expensive right now - don't abandon the whole run, just
            // exclude it from the next pick so cheaper (possibly non-priority) nodes still get a try.
            yield return CurrencyMissingPopup.Close;
            unaffordableThisRun.Add(best.Value);
        }

        yield return TreeOfLife.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private static int? FindBestUpgrade(ICollection<int> unaffordableThisRun)
    {
        int? bestPriority = null;
        var bestPriorityLevel = int.MaxValue;
        int? bestOther = null;
        var bestOtherLevel = int.MaxValue;

        for (var i = 0; i < TreeOfLife.PersonalUpgradeCount; i++)
        {
            if (unaffordableThisRun.Contains(i)) continue;
            if (!TreeOfLife.PersonalNode(i).IsClickable()) continue;

            var level = TreeOfLife.PersonalNodeLevel(i);
            if (level >= MaxUpgradeLevel) continue;

            if (TreeOfLife.IsPriority(i))
            {
                if (level >= bestPriorityLevel) continue;
                bestPriorityLevel = level;
                bestPriority = i;
            }
            else
            {
                if (level >= bestOtherLevel) continue;
                bestOtherLevel = level;
                bestOther = i;
            }
        }

        // A priority candidate always wins if one is currently affordable and not maxed, regardless
        // of the cheapest non-priority level - but only among candidates not already confirmed
        // unaffordable this run, so a too-expensive priority group falls back to the other 17.
        return bestPriority ?? bestOther;
    }
}
