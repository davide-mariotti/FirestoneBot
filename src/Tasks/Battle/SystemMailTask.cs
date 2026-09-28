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

    private const int MaxIterations = 200;

    public override IEnumerator Execute()
    {
        yield return SystemMail.Open;

        var index = 0;
        var iterations = 0;

        while (index < SystemMail.MailList.GetChildren().Count() && iterations < MaxIterations)
        {
            iterations++;
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
