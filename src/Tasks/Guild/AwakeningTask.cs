using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Spends Arcane Crystals on Awakening (Guild -&gt; Awakening) - claimed via the Mailbox task
///     (SystemMailTask) since the wiki lists Arcane Crystal rewards as mail deliveries, then spent
///     here. Level 50 gate per the wiki. This whole feature is new, requested by
///     the user.
///     No selection logic needed at all: per the wiki, the awakened hero is chosen randomly by the
///     game itself, weighted toward heroes with a lower awakening level (catch-up mechanism similar
///     to Sacred Cards) - nothing for this task to decide there.
///     Per the user ("cercando di usare il moltiplicatore il più possibile"): always re-selects the
///     biggest currently-usable multiplier (x160 down to x1, x1 always available) before every single
///     awaken, not just once at the start - the multiplier only affects cost/xp proportionally (no
///     actual efficiency gain per the wiki), so this is purely about spending crystals in as few
///     clicks as possible, and re-picking each time naturally steps down to a smaller multiplier once
///     the bigger one is no longer affordable instead of stalling out early.
/// </summary>
public class AwakeningTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 50;

    // Live-confirmed real, 2026-09-20 (BotManager's notification rail diagnostic dump). No threshold
    // gate here (unlike FreePickaxes/Empower/Meteorite Research) - this task spends every Arcane
    // Crystal on hand unconditionally, so "badge lit" and "actionable now" are the same thing.
    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Awakening;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // Safety bound only, matching the pattern used everywhere else in this codebase for a "keep
    // going until nothing's left to do" loop.
    private const int MaxIterations = 200;

    public override IEnumerator Execute()
    {
        // Fast path - opportunistic only, safe no-op if not up (see NotificationBadgeName above).
        yield return Notifications.Awakening;

        yield return TownGuild.Open;
        yield return TownGuild.OpenAwakening;

        for (var i = 0; i < MaxIterations; i++)
        {
            yield return Awakening.SelectBestMultiplier();

            if (!Awakening.AwakenBtn.IsClickable()) break;

            yield return Awakening.Awaken();
        }

        yield return Awakening.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
