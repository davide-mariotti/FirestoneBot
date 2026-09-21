namespace Firebot.Infrastructure;

/// <summary>
///     In-game mailbox (internal screen name "SystemMail", not "Mailbox") - delivers Arcane Crystal
///     rewards, character-level milestone rewards, Arena of Kings rank rewards and unclaimed Battle
///     Pass rewards at month-end (per the wiki). This whole feature is new, found via
///     a targeted UnityPy scan requested by the user. Opened directly from the battle screen (see
///     Battle.cs LeftSideUINewLoc.MailBtn), not through Town.
/// </summary>
public static partial class Paths
{
    // Root path convention (popups/X) not independently confirmed for this specific screen (same
    // situation as several other screens this session, e.g. WFBattleSim) - the prefab has no live
    // parent captured in the static asset dump, so this follows the same convention every other
    // popup opened directly over the battle view already uses.
    public static class SystemMailLoc
    {
        private const string Root = MenusLoc.Root + "/popups/SystemMail";

        public const string CloseBtn = Root + "/bg/closeButton";

        // Scrollable list of mail items (mailOnList (N) in the static prefab, but scanned generically
        // here rather than assuming a fixed count - confirmed via UnityPy this is a normal scrolling
        // list, not a fixed set of slots like the quest tabs).
        public const string MailListRoot = Root + "/bg/mailListBg/Scroll View/Viewport/mailList";

        // A single shared detail panel, NOT a separate popup per mail - sibling of mailListBg under
        // the same screen, confirmed via UnityPy. Selecting any list item updates this same panel, so
        // there's no "close the detail view" step between mails, only between the whole screen.
        // Only wired to claimButton - NEVER deleteButton (also found here, right next to it): the
        // user only asked for claiming, and deleting is a destructive, unrequested action - leaving
        // read/claimed mail in the list is harmless, re-clicking an already-claimed one is just
        // another safe no-op like everywhere else in this codebase.
        public const string ClaimBtn = Root + "/bg/mailFullView/mailMessage/rewardsObj/claimButton";
    }
}
