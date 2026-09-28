namespace Firebot.Infrastructure;

/// <summary>The formation editor, only read to know who is in the 5 active slots. Not verified live.</summary>
public static partial class Paths
{
    public static class PartyLoc
    {
        private const string Root = MenusLoc.Root + "/menus/Party";

        public const string CloseBtn = Root + "/closeButton";

        // Each "deckSlot(N)" card shows "bg/activeIcon" while that hero is in the formation. Assumed,
        // not verified, to list heroes in the same order as Hall of Heroes' own grid.
        public const string HeroRosterRoot = Root + "/bg/deck/heroScroll/Viewport/heroGrid";
    }
}
