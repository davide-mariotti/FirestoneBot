using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Logger = Firebot.Core.Logger;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Spends the day's Arena of Kings tokens (5) on the weakest opponent it can find. A loss doesn't
///     lower the rank - only a win changes it - so a token is never left unused: each one gets up to
///     12 looks at the 3 opponents (a reroll is free every 5 s), accepting a slightly stronger
///     opponent the longer it searches (see SearchPhases), and fights the weakest on the last look.
///     The formation is set up by hand once; the task never touches it.
/// </summary>
public class ArenaOfKingsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 80;

    // Five real battles plus their searches can outlast the global task timeout.
    internal override float? MaxRuntimeSeconds => 3600f;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ArenaTokens;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // Generous bounds, not measured durations.
    private const int MaxBattlePolls = 150;
    private const float BattlePollSeconds = 2f;
    private const int MaxRerollPolls = 40;
    private const float RerollPollSeconds = 0.5f;

    private const int MaxSearchAttemptsPerToken = 12;

    // (looks already taken below which this phase applies, max opponent power as a multiple of
    // mine - null means strictly weaker only). The first matching phase wins.
    private static readonly (int AttemptThreshold, double? MaxMultiplier)[] SearchPhases =
    {
        (4, null),
        (7, 1.05),
        (9, 1.10),
        (12, 1.20)
    };

    public override IEnumerator Execute()
    {
        yield return Notifications.ArenaTokens;

        yield return TownScreen.Open;
        yield return TownScreen.OpenBattles;
        yield return WFMenuSelection.OpenArena;

        while (ArenaOfKings.TokensAvailable > 0)
        {
            var slotIndex = -1;
            yield return FindTarget(index => slotIndex = index);
            if (slotIndex < 0) break; // opponents not shown

            yield return ArenaOfKings.Fight(slotIndex);
            yield return AOKBattlePreview.Fight;

            yield return Poll.Until(() => AOKBattleResult.IsVisible, MaxBattlePolls, BattlePollSeconds);
            if (!AOKBattleResult.IsVisible)
                Logger.Warning("[ArenaOfKingsTask] Battle didn't resolve within the wait bound.");

            yield return AOKBattleResult.Close;

            // The hub redraws for a moment after the result closes - its texts read blank meanwhile.
            yield return WaitForHub();
        }

        yield return ArenaOfKings.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private static IEnumerator WaitForHub() =>
        Poll.Until(() => ArenaOfKings.RerollBtn.IsClickable(), MaxRerollPolls, RerollPollSeconds);

    private IEnumerator FindTarget(Action<int> onFound)
    {
        var attempt = 0;

        while (true)
        {
            // The opponents can stay hidden after a reroll or a battle, read as 0 - a blank isn't a weak
            // opponent (01/10: Steam-3 and -7 lost 5 minutes on it, Steam-2 the whole hour). Reopening
            // the arena on the next run (the ArenaTokens badge) redraws them.
            yield return Poll.Until(() => ArenaOfKings.OpponentPowers().All(p => p > 0), MaxRerollPolls, RerollPollSeconds);
            var myPower = ArenaOfKings.MyPower;
            var powers = ArenaOfKings.OpponentPowers();
            if (powers.Any(p => p <= 0))
            {
                Debug($"[INFO] Arena: {ArenaOfKings.TokensAvailable} token(s), opponents not shown " +
                      $"({string.Join(" / ", powers)}) - left for the next run.");
                yield break;
            }

            var chosen = ChooseOpponent(myPower, powers, attempt);
            if (chosen != null)
            {
                Debug($"[INFO] Arena: {ArenaOfKings.TokensAvailable} token(s), look {attempt + 1}: my power {myPower}, " +
                      $"opponents {string.Join(" / ", powers)} -> slot {chosen.Value}.");
                onFound(chosen.Value);
                yield break;
            }

            attempt++;
            yield return ArenaOfKings.Reroll;
            yield return WaitForHub();
        }
    }

    private static int? ChooseOpponent(double myPower, IReadOnlyList<double> powers, int attemptsSoFar)
    {
        // The last look always picks, so the search ends within MaxSearchAttemptsPerToken looks.
        if (attemptsSoFar >= MaxSearchAttemptsPerToken - 1)
            return WeakestIndex(powers);

        foreach (var (attemptThreshold, maxMultiplier) in SearchPhases)
        {
            if (attemptsSoFar >= attemptThreshold) continue;
            return WeakestWithin(powers, myPower, maxMultiplier);
        }

        return WeakestIndex(powers); // unreachable with the phases above; kept as a safety net
    }

    private static int? WeakestWithin(IReadOnlyList<double> powers, double myPower, double? maxMultiplier)
    {
        int? best = null;
        var bestPower = double.MaxValue;

        for (var i = 0; i < powers.Count; i++)
        {
            var acceptable = maxMultiplier == null ? powers[i] < myPower : powers[i] <= myPower * maxMultiplier.Value;
            if (!acceptable || powers[i] >= bestPower) continue;

            bestPower = powers[i];
            best = i;
        }

        return best;
    }

    private static int WeakestIndex(IReadOnlyList<double> powers)
    {
        var best = 0;
        for (var i = 1; i < powers.Count; i++)
            if (powers[i] < powers[best])
                best = i;
        return best;
    }
}
