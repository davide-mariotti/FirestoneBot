using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.Utilities;
using MelonLoader;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     The daily "Gamer" quest: 10 Tavern card draws, paid in game tokens (BeerExchangeTask turns
///     beer into tokens). Plays all remaining draws in one round when the quantity selector offers
///     that exact number, and never goes below min_token_reserve - except once per game-day, when the
///     reserve is the only thing keeping the quest from finishing. Once the 10 draws are done, the
///     task waits for the next game-day.
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

        // One bulk round when the reserve leaves room for all of it (a play costs 1 token per draw).
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
                yield return Tavern.TrySetPlayQuantityTo(1); // the loop below plays one draw at a time
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
