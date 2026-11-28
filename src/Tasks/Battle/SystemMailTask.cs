using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.Tasks.Battle;

/// <summary>
///     Claims every reward in the mailbox and opens every unread mail, so the mail badge goes off.
///     Whether a claimed mail leaves the list isn't known, so the scan works either way: it stays on an
///     index until that mail has nothing left to claim. Mail is never deleted.
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

    // Something for the mailbox: a reward to claim or a mail not read yet (the badge lights for both;
    // 28/11 mail without rewards stayed unread, badge lit, as only rewards opened it).
    // Unread mails a run couldn't mark read don't count again until there are more of them.
    private int? Pending => GameState.ClaimableMails is { } claimable && GameState.UnseenMails is { } unseen
        ? claimable + (unseen > _unreadLeft ? unseen : 0)
        : null;

    private int _unreadLeft;

    // The game's mailbox says when there is something to do: mail turns up at any time, and every
    // 6-hour run opened all of it (38-72 s on 26/11) to claim nothing most times.
    public override bool IsNotificationVisible() => Pending is { } pending
        ? IsEnabled && MeetsLevelRequirement && pending > 0
        : base.IsNotificationVisible();

    private const int MaxIterations = 200;

    public override IEnumerator Execute()
    {
        if (Pending == 0)
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
            if (Pending == 0) break; // everything claimed and read: the rest need no click
            var mail = SystemMail.MailList.GetChild(index);
            if (mail == null) break;

            yield return new GameButton(parent: mail).Click();

            var claimBtn = SystemMail.ClaimBtn;
            if (claimBtn.IsClickable())
                yield return claimBtn.Click();
            else
                index++; // nothing (left) to claim here, and now read
        }

        _unreadLeft = GameState.UnseenMails ?? 0;
        Debug($"[INFO] Mail: {GameState.ClaimableMails} to claim, {_unreadLeft} unread left.");
        yield return SystemMail.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
