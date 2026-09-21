namespace Firebot.Infrastructure;

/// <summary>
///     Battle formation editor ("Party" screen) - opened via BattleLoc.BottomSideUIDesktopLoc.PartyBtn.
///     Used only to read which heroes are currently in the active 5-slot formation
///     (GameModel/Features/Character/Party.cs), never to change it. Root path convention inferred
///     from Inventory (opened the same way, from the same HUD row) - not independently confirmed live.
/// </summary>
public static partial class Paths
{
    public static class PartyLoc
    {
        private const string Root = MenusLoc.Root + "/menus/Party";

        public const string CloseBtn = Root + "/closeButton";

        // The hero roster you drag FROM into the 5 active formation slots (bg/heroSlots, not read by
        // this codebase) - each card is "deckSlot(N)" with a "bg/activeIcon" badge shown only when
        // that hero currently occupies one of the 5 slots, confirmed via UnityPy. Assumed (not
        // independently verified live) to list heroes in the same order as Hall of Heroes' own
        // roster grid, since neither list exposes a readable hero name to cross-check by - flag for
        // live verification before trusting HallOfHeroesGearTask's T1 targeting unsupervised.
        public const string HeroRosterRoot = Root + "/bg/deck/heroScroll/Viewport/heroGrid";
    }
}
