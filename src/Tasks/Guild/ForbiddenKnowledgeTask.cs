using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Il2Cpp;
using CurrencyMissingPopup = Firebot.GameModel.Shared.CurrencyMissingPopup;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Forbidden Knowledge: on each of the three boards, upgrades every node its own god's Tomes of
///     Power can pay for, up to the board's cap, then tries "Recruit", which only works once the board
///     is maxed. The game's data says which boards have anything to do, so the rest stay closed.
/// </summary>
public class ForbiddenKnowledgeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 100;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ForbiddenKnowledge;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // The tabs' order on screen.
    private static readonly (DarkGods God, Func<IEnumerator> Open)[] Boards =
    {
        (DarkGods.Kramatak, () => ForbiddenKnowledge.OpenKramatakTab),
        (DarkGods.Ledra, () => ForbiddenKnowledge.OpenLedraTab),
        (DarkGods.Yamanoth, () => ForbiddenKnowledge.OpenYamanothTab)
    };

    // Nothing to do on the board, by the game's data: no node below the cap a level is paid for, no recruit.
    private static bool Idle((int Tomes, int Open, int Cost, bool Recruit)? board) =>
        board is { } b && !b.Recruit && (b.Open == 0 || b.Tomes < b.Cost);

    public override IEnumerator Execute()
    {
        // A lit badge is the game's own word that something's there: then every board is looked at.
        var badge = IsNotificationVisible();
        var boards = Boards.Select(b => (b.God, b.Open, Data: ForbiddenKnowledge.Board(b.God))).ToList();
        if (!badge && boards.All(b => Idle(b.Data)))
        {
            // 71-80 s a run before, opening every node's preview to buy nothing (TESTING.md, Steam-26 27/11).
            Debug("[INFO] From the game: " + string.Join(", ", boards.Select(b =>
                $"{b.God} {b.Data?.Tomes} tomes, {b.Data?.Open} open, {b.Data?.Cost} each")) + " - not opening.");
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return Notifications.ForbiddenKnowledge;

        yield return TownGuild.Open;
        yield return TownGuild.OpenForbiddenKnowledge;

        if (!ForbiddenKnowledge.IsVisible)
        {
            // Usually a timing hiccup: no NextRunTime, so the scheduler's short idle retry applies.
            yield return TownGuild.Close;
            yield break;
        }

        foreach (var (god, open, data) in boards)
        {
            if (!badge && Idle(data)) continue;

            yield return open();
            yield return UpgradeBoard(god);
            yield return ForbiddenKnowledge.TryRecruit();
        }

        yield return ForbiddenKnowledge.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private IEnumerator UpgradeBoard(DarkGods god)
    {
        var bought = 0;
        var start = ForbiddenKnowledge.Board(god)?.Tomes;
        for (var i = 0; i < Paths.ForbiddenKnowledgeLoc.NodeCount; i++)
        {
            var node = ForbiddenKnowledge.Node(i);
            // Each node up to the board's cap; at most 5 levels (one step of the cap) per node a run.
            for (var level = 0; level < 5 && node.IsClickable() && ForbiddenKnowledge.BelowCap(i); level++)
            {
                var before = ForbiddenKnowledge.Board(god);
                if (before is { } b && b.Tomes < b.Cost)
                {
                    Log(god, start, bought);
                    yield break;
                }

                yield return node.Click();
                yield return ForbiddenKnowledge.ConfirmUpgrade();

                if (CurrencyMissingPopup.IsShowing)
                {
                    yield return CurrencyMissingPopup.Close;
                    Log(god, start, bought);
                    yield break; // this board's tomes ran out
                }

                // The tomes drop when the server confirms; unchanged means nothing was bought.
                yield return Poll.Until(() => ForbiddenKnowledge.Board(god)?.Tomes != before?.Tomes, 30, 0.1f);
                if (before != null && ForbiddenKnowledge.Board(god)?.Tomes == before.Value.Tomes) break;
                bought++;
            }
        }

        Log(god, start, bought);
    }

    private void Log(DarkGods god, int? start, int bought) =>
        Debug($"[INFO] Forbidden Knowledge: {god} tomes {start} -> {ForbiddenKnowledge.Board(god)?.Tomes}, {bought} level(s) bought.");
}
