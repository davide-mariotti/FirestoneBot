using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Battle;

/// <summary>
///     Claims every reward in the mailbox. Whether a claimed mail leaves the list isn't known, so the
///     scan works either way: it stays on an index until that mail has nothing left to claim. Mail is
///     never deleted.
/// </summary>
public class SystemMailTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    // The mail badge's path is a guess (see MailNotification); without it this still runs every 6 hours.
    protected override string[] NotificationPaths => new[]
    {
        Paths.BattleLoc.LeftSideUINewLoc.MailNotification,
        Paths.BattleLoc.BottomLeftSideUILoc.MailNotification
    };

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // The game's mailbox says when there is something to claim: mail turns up at any time, and every
    // 6-hour run opened all of it (38-72 s on 26/11) to claim nothing most times.
    public override bool IsNotificationVisible() => GameState.ClaimableMails is { } claimable
        ? IsEnabled && MeetsLevelRequirement && claimable > 0
        : base.IsNotificationVisible();

    private const int MaxIterations = 200;

    public override IEnumerator Execute()
    {
        if (GameState.ClaimableMails == 0)
        {
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        yield return SystemMail.Open;

        var index = 0;
        var iterations = 0;

        while (index < SystemMail.MailList.GetChildren().Count() && iterations < MaxIterations)
        {
            iterations++;
            if (GameState.ClaimableMails == 0) break; // everything claimed: the rest need no click
            var mail = SystemMail.MailList.GetChild(index);
            if (mail == null) break;

            yield return new GameButton(parent: mail).Click();

            var claimBtn = SystemMail.ClaimBtn;
            if (claimBtn.IsClickable())
                yield return claimBtn.Click();
            else
                index++; // nothing (left) to claim here
        }

        yield return SystemMail.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
