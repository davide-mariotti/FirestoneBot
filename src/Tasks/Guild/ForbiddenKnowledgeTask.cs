using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Guild;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Guild;

/// <summary>
///     Forbidden Knowledge (Town -&gt; Guild -&gt; Forbidden Knowledge, level 100) - 3 separate upgrade
///     boards (Kramatak/Ledra/Yamanoth), each spending only that same god's Tomes of Power (earned via
///     ChaosRiftTask). For each board: upgrades every clickable node (stops early on that board if
///     tomes run out, via the shared CurrencyMissingPopup - same safety pattern as TreeOfLife/War
///     Machines), then tries "Recruit [God]" (safe no-op unless every node is already maxed and 50
///     tomes are on hand, per the wiki).
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
            // A one-off timing hiccup rather than a real problem - don't hardcode NextRunTime here,
            // let BotManager's own default idle-retry floor apply instead.
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
            yield break; // out of this board's Tomes of Power - no point checking the rest
        }
    }
}
