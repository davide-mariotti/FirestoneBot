using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild.Expeditions;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Guild;

/// <summary>Collects the finished guild expedition and starts the first pending one.</summary>
public class ExpeditionTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 10;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Expeditions;

    // The claim opens a moment after the game's end time.
    private static readonly TimeSpan EndMargin = TimeSpan.FromSeconds(2);

    public override IEnumerator Execute()
    {
        // The screen's countdown ends early: on 27/11 Steam-27 opened at 12:45 and 13:32 with the claim still
        // off, and came back 2 minutes later. The game's end (13:32:15) was a second after that visit.
        if (!IsNotificationVisible() && Expedition.EndFromGame is { } end && end > DateTime.Now)
        {
            NextRunTime = end + EndMargin;
            Debug($"[INFO] From the game: the expedition ends {end:HH:mm:ss} - not opening.");
            yield break;
        }

        yield return Notifications.Expeditions;

        yield return TownGuild.Open;
        yield return TownGuild.OpenExpeditions;

        yield return Expedition.Claim;
        yield return Expedition.StartBest();

        // With none running the screen's countdown is the list's refresh, which the data doesn't hold.
        var fromScreen = Expedition.IsExpeditionActive ? Expedition.CurrentRunTime : Expedition.NextRunTime;
        var fromGame = Expedition.EndFromGame is { } e && e != DateTime.MinValue ? e : (DateTime?)null;
        GameState.CompareTimes("expedition end", fromGame, fromScreen);
        NextRunTime = fromGame + EndMargin ?? fromScreen;
        Debug($"[INFO] Expedition: next {NextRunTime:HH:mm:ss} (game {fromGame:HH:mm:ss}, screen {fromScreen:HH:mm:ss}).");

        yield return Expedition.Close;
        yield return TownGuild.Close;
    }
}
