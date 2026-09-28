namespace Firebot.GameModel.Features.Events;

/// <summary>
///     What to spend event currency on in an event shop's Exchange tab, best value first - shared by
///     every event task so the two (today DecoratedHeroesEventTask and NewPlayerEventTask) can't
///     drift apart when the policy changes. Same idea as TalentBuildConfig: the decision lives next
///     to its feature, not duplicated in each task that acts on it.
///     Order per the F2P guide (see PLAN.md, "Allineamento alla guida F2P" 1.2): Dragon Blood and
///     Meteorites are the two items it names as worth buying in event shops; Beer is on its
///     "avoid" list and sits here only as a last-resort sink, since event currency expires when the
///     event ends and spending it badly still beats not spending it.
///     Not every name exists in every shop, deliberately: matching is a case-insensitive partial
///     match on the item's displayed name, and a miss is a logged no-op (see each shop's BuyItem).
///     Decorated Heroes has Dragon blood but no Meteorite; New Player has Meteorite but no Dragon
///     blood. Both tasks walk this same list and each picks up whatever its own shop happens to
///     offer.
///     Behaves as a cascade rather than a strict priority, which is the intended effect: BuyItem
///     keeps buying one item until its "Claimed X/50" cap or the currency runs out, so leftovers
///     naturally roll down to the next name instead of sitting unspent. Once the currency is gone,
///     each remaining name costs one click and one CurrencyMissingPopup dismissal - negligible at
///     the tasks' hourly cadence.
///     Kept hardcoded on purpose: if the order needs changing a third time, promote it to a cfg
///     entry then, not before.
/// </summary>
public static class EventExchangeConfig
{
    public static readonly string[] PriorityItems = { "Dragon blood", "Meteorite", "Beer" };
}
