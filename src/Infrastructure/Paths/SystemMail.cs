namespace Firebot.Infrastructure;

/// <summary>The in-game mailbox, opened straight from the battle screen.</summary>
public static partial class Paths
{
    public static class SystemMailLoc
    {
        private const string Root = MenusLoc.Root + "/popups/SystemMail";

        public const string CloseBtn = Root + "/bg/closeButton";

        public const string MailListRoot = Root + "/bg/mailListBg/Scroll View/Viewport/mailList";

        // One shared detail panel for whichever mail is selected. Only its claim button is wired;
        // the deleteButton next to it is never touched.
        public const string ClaimBtn = Root + "/bg/mailFullView/mailMessage/rewardsObj/claimButton";
    }
}
