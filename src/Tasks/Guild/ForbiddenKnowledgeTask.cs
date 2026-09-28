using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Forbidden Knowledge: on each of the three boards, upgrades every node its own god's Tomes of
///     Power can pay for, then tries "Recruit", which only works once the board is maxed.
/// </summary>
public class ForbiddenKnowledgeTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Guild;
    protected override int MinimumCharacterLevel => 100;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ForbiddenKnowledge;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    public override IEnumerator Execute()
    {
        yield return Notifications.ForbiddenKnowledge;

        yield return TownGuild.Open;
        yield return TownGuild.OpenForbiddenKnowledge;

        if (!ForbiddenKnowledge.IsVisible)
        {
            // Usually a timing hiccup: no NextRunTime, so the scheduler's short idle retry applies.
            yield return TownGuild.Close;
            yield break;
        }

        yield return ForbiddenKnowledge.OpenKramatakTab;
        yield return ClaimAllNodes();
        yield return ForbiddenKnowledge.TryRecruit();

        yield return ForbiddenKnowledge.OpenLedraTab;
        yield return ClaimAllNodes();
        yield return ForbiddenKnowledge.TryRecruit();

        yield return ForbiddenKnowledge.OpenYamanothTab;
        yield return ClaimAllNodes();
        yield return ForbiddenKnowledge.TryRecruit();

        yield return ForbiddenKnowledge.Close;
        yield return TownGuild.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private static IEnumerator ClaimAllNodes()
    {
        for (var i = 0; i < Paths.ForbiddenKnowledgeLoc.NodeCount; i++)
        {
            var node = ForbiddenKnowledge.Node(i);
            if (!node.IsClickable()) continue;

            yield return node.Click();
            yield return ForbiddenKnowledge.ConfirmUpgrade();

            if (!CurrencyMissingPopup.IsShowing) continue;
            yield return CurrencyMissingPopup.Close;
            yield break; // this board's tomes ran out
        }
    }
}
