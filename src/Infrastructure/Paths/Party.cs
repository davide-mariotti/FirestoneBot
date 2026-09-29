namespace Firebot.Infrastructure;

/// <summary>The formation editor, only read to know who is in the formation. Verified live on 2026-09-29.</summary>
public static partial class Paths
{
    public static class PartyLoc
    {
        private const string Root = MenusLoc.Root + "/menus/Party";

        public const string CloseBtn = Root + "/closeButton";

        // "heroSlot0".."heroSlot4". A deployed hero's spine is a child of its slot named after the hero
        // as the Hall of Heroes shows it ("heroSlot4/Cirilo"). They load one by one: 2 of 5 were there
        // 1-3 s after opening, all 5 after about 2-4 s, and "Deployed: N/5" counts up with them, so it
        // can't tell when the loading is over. The deck below lists heroes in another order than the
        // Hall of Heroes, and its tick icons fill in just as late.
        public const string HeroSlotsRoot = Root + "/bg/parallaxBg/layers/heroSlots";
    }
}
