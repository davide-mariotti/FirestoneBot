using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.Infrastructure;
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
/// </summary>
public class GamerQuestTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Quests;
    protected override int MinimumCharacterLevel => 15;

    private const int PlayCount = 10;
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private MelonPreferences_Entry<int> _minTokenReserve;
    private MelonPreferences_Entry<string> _reserveOverrideUsedDate;

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
    }

    public override IEnumerator Execute()
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenTavern;

        var minReserve = _minTokenReserve?.Value ?? 10;
        var playsDone = 0;

        // Only attempted with enough headroom above the reserve for the full x10 cost (confirmed
        // linear: 10 tokens) - if that's wrong for some reason, the game's own affordability gate on
        // the button keeps it non-clickable and this safely falls through to the per-round loop below.
        if (Tavern.GameTokenCount - minReserve >= PlayCount)
        {
            yield return Tavern.TrySetPlayQuantityTo(PlayCount);

            if (Tavern.IsPlayQuantitySetTo(PlayCount) && Tavern.PlayBtn.IsClickable())
            {
                yield return Tavern.PlayRound();
                playsDone += PlayCount;
            }
            else
            {
                yield return Tavern.TrySetPlayQuantityTo(1); // revert so the per-round loop below is correct
            }
        }

        while (playsDone < PlayCount && Tavern.GameTokenCount > minReserve)
        {
            if (!Tavern.PlayBtn.IsClickable()) break;

            yield return Tavern.PlayRound();
            playsDone++;
        }

        var today = DateTime.Now.ToString("yyyy-MM-dd");
        if (playsDone < PlayCount && _reserveOverrideUsedDate?.Value != today)
        {
            var playsBeforeOverride = playsDone;

            while (playsDone < PlayCount && Tavern.GameTokenCount > 0)
            {
                if (!Tavern.PlayBtn.IsClickable()) break;

                yield return Tavern.PlayRound();
                playsDone++;
            }

            if (playsDone > playsBeforeOverride && _reserveOverrideUsedDate != null)
                _reserveOverrideUsedDate.Value = today;
        }

        yield return Tavern.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
