namespace Firebot.Infrastructure;

/// <summary>
///     Pirate Ship building (https://firestone-idle-rpg.fandom.com/wiki/Pirate_Ship), unlocks at
///     character level 10 per the wiki. Only the "Pirate's Prize" tab (a free/paid reward track,
///     gated by character level) is automated - "Mercenaries" (hire/recruit, spends currency),
///     "Captain's Deal" (real-money bundle), and "Skins" are all purchase/cosmetic flows, out of
///     scope. Paths cross-checked against docs/screens/PirateShip.html (a static UnityPy dump) then
///     live-confirmed, 2026-09-18: the dump's own root-path guess ("menus/PirateShip") was correct
///     this time (unlike several other screens this session where "menus" vs "popups" guessed wrong).
/// </summary>
public static partial class Paths
{
    public static class PirateShipLoc
    {
        private const string Root = MenusLoc.Root + "/menus/PirateShip";

        public const string CloseBtn = Root + "/closeButton";

        // Live-confirmed, 2026-09-18: "submenus/piratesPrize" is already active=True the instant the
        // screen opens, before this is ever clicked - unlike BattlePass's RewardsTabBtn, which
        // genuinely needs an explicit click. Kept for reference only - NOT called by
        // PiratesPrizeTask, since clicking it was observed resetting the reward track's scroll
        // position back to its start.
        public const string PiratesPrizeTabBtn = Root + "/bg/submenuButtons/piratesPrize/button";

        public static class PiratesPrizeLoc
        {
            private const string SubmenuRoot = Root + "/bg/submenus/piratesPrize";

            // Live-confirmed, 2026-09-18 (3 rounds of live diagnostics): NOT actually pooled/
            // virtualized despite first appearances - all ~20 "ppTierInteraction(Clone)" children
            // exist simultaneously regardless of scroll position, the ScrollRect just visually pans
            // over them. But every one of those ~20 siblings shares the EXACT SAME name (no
            // per-instance index, unlike every other pooled list in this codebase) - GameElement/
            // GameButton's string-path re-resolution can't address one specific sibling among
            // identically-named ones, so PiratesPrizeTask resolves this root once via a raw
            // Transform lookup and walks its children directly instead - see that task's own doc
            // comment for the full story.
            public const string TierListRoot = SubmenuRoot + "/Scroll View/Viewport/content/rewardsTierSection";
        }
    }
}
