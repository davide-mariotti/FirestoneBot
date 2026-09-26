using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Progresses the daily quest "Gamer" (play 10 times in the Tavern) - level 15 per the wiki quest
///     table. Never automated before.
///     Card draws cost game tokens (NOT beer, despite the name suggesting otherwise - confirmed via
///     the wiki: "Card draws require Game Tokens"). Plays up to 10 times but always leaves at least
///     min_token_reserve tokens unspent, so tomorrow's 10 plays aren't blocked either. See
///     BeerExchangeTask for how tokens get topped up from passively-accumulated beer.
///     Live-confirmed, 2026-09-18 (user screenshots): "Play 1" costs 1 token, "Play 10" costs 10
///     (linear) - user-requested optimization: use the x10 multiplier for one round instead of 10
///     separate x1 rounds whenever there's enough headroom above the reserve for it, same idea as
///     Miner Quest's ArcaneCrystal quantity shortcut. Each round is Play + picking one of the
///     resulting card stacks (see Tavern.PlayRound) - Play alone doesn't complete anything.
///     Per the user (2026-09-20): raised the reserve to 10 (was 5), but the reserve can be bypassed
///     ONCE PER DAY if it's still blocking today's quest from completing - a slow token-income day
///     shouldn't cost a whole day's quest. Tracked via reserve_override_used_date (today's date once
///     spent) so it can't be reused on a later run the same day; only counts as "used" once it
///     actually plays an extra round, so a day with genuinely 0 tokens left keeps the bypass available
///     for a later run once some tokens trickle back in.
///     Per the user (2026-09-23): once all 10 plays for today are done, skip the whole routine (no
///     Tavern screen open, no token/quantity reads) until the date changes - previously this reopened
///     Tavern and re-checked everything every 6h even on a day already fully played, wasted clicks
///     across 16 bot instances. Tracked via plays_done_today/plays_done_date, separate from the
///     reserve override tracking above (a day can finish its 10 plays without ever needing the
///     override).
/// </summary>
public class GamerQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;
    protected override int MinimumCharacterLevel => 15;

    private const int PlayCount = 10;
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private MelonPreferences_Entry<int> _minTokenReserve;
    private MelonPreferences_Entry<string> _reserveOverrideUsedDate;
    private MelonPreferences_Entry<int> _playsDoneToday;
    private MelonPreferences_Entry<string> _playsDoneDate;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_minTokenReserve != null) return;

        _minTokenReserve = category.CreateEntry(
            "min_token_reserve",
            10,
            "Minimum Game Token Reserve",
            "Never spend game tokens on Tavern card draws below this count, so tomorrow's 10 draws " +
            "for this quest aren't blocked either. Default: 10. Can be bypassed once per day if it's " +
            "still blocking today's quest from completing - see reserve_override_used_date."
        );

        _reserveOverrideUsedDate = category.CreateEntry(
            "reserve_override_used_date",
            "",
            "Reserve Override Last Used",
            "(auto-managed, don't edit) - the last date the token reserve was bypassed to finish " +
            "today's quest. Resets automatically once the date changes."
        );

        _playsDoneToday = category.CreateEntry(
            "plays_done_today",
            0,
            "Plays Done Today",
            "(auto-managed, don't edit) - how many of today's 10 card draws are already done. " +
            "Resets automatically once the date changes."
        );

        _playsDoneDate = category.CreateEntry(
            "plays_done_date",
            "",
            "Plays Done Date",
            "(auto-managed, don't edit) - the date plays_done_today is counting for."
        );
    }

    public override IEnumerator Execute()
    {
        var today = GameDay.Today();

        if (_playsDoneDate?.Value != today)
        {
            if (_playsDoneToday != null) _playsDoneToday.Value = 0;
            if (_playsDoneDate != null) _playsDoneDate.Value = today;
        }

        if (_playsDoneToday?.Value >= PlayCount)
        {
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return TownScreen.Open;
        yield return TownScreen.OpenTavern;

        var minReserve = _minTokenReserve?.Value ?? 10;
        var alreadyDone = _playsDoneToday?.Value ?? 0;
        var remaining = PlayCount - alreadyDone;
        var playsDone = 0;

        // Only attempted with enough headroom above the reserve for the full remaining-plays cost
        // (confirmed linear: 1 token/play) - if that's wrong for some reason, the game's own
        // affordability gate on the button keeps it non-clickable and this safely falls through to
        // the per-round loop below.
        if (remaining > 1 && Tavern.GameTokenCount - minReserve >= remaining)
        {
            yield return Tavern.TrySetPlayQuantityTo(remaining);

            if (Tavern.IsPlayQuantitySetTo(remaining) && Tavern.PlayBtn.IsClickable())
            {
                yield return Tavern.PlayRound();
                playsDone += remaining;
            }
            else
            {
                yield return Tavern.TrySetPlayQuantityTo(1); // revert so the per-round loop below is correct
            }
        }

        while (playsDone < remaining && Tavern.GameTokenCount > minReserve)
        {
            if (!Tavern.PlayBtn.IsClickable()) break;

            yield return Tavern.PlayRound();
            playsDone++;
        }

        if (playsDone < remaining && _reserveOverrideUsedDate?.Value != today)
        {
            var playsBeforeOverride = playsDone;

            while (playsDone < remaining && Tavern.GameTokenCount > 0)
            {
                if (!Tavern.PlayBtn.IsClickable()) break;

                yield return Tavern.PlayRound();
                playsDone++;
            }

            if (playsDone > playsBeforeOverride && _reserveOverrideUsedDate != null)
                _reserveOverrideUsedDate.Value = today;
        }

        if (_playsDoneToday != null) _playsDoneToday.Value = alreadyDone + playsDone;

        yield return Tavern.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
