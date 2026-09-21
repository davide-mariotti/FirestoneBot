namespace Firebot.Infrastructure;

/// <summary>
///     Scarab's Game (a slot-machine minigame) and its shop. No prior precedent at all. Corrected
///     twice: first from "no permanent manual entry point" to "Town -&gt; Tavern -&gt; Tavern's own 'shop'
///     button" (per the wiki), then live-confirmed 2026-09-18 that the real route is Town -&gt; the
///     "tavern" building's TavernSelection choice popup -&gt; its "scarabGame" card directly - it never
///     actually goes through the card-flip Tavern screen at all (see Town.OpenScarabGame). Unlocks at
///     character level 60 per the wiki (Tavern itself unlocks earlier, at level 15).
/// </summary>
public static partial class Paths
{
    public static class ScarabGameLoc
    {
        // Public (not private) so a diagnostic dump can enumerate its children directly - see
        // PharaohsVaultTask.DumpMilestonesDiagnostics.
        public const string Root = MenusLoc.Root + "/menus/ScarabGame";

        public const string CloseBtn = Root + "/closeButton";

        public const string OpenShopBtn = Root + "/helpCanvas/actionButtons/shop";

        public const string OpenVaultBtn = Root + "/helpCanvas/actionButtons/pharaohVault";

        // NOT yet confirmed - guessed as a third sibling of shop/pharaohVault above (same
        // "helpCanvas/actionButtons/" container). Opens the Milestones popup (ScarabGameMilestonesLoc
        // below), per the user's screenshot, 2026-09-20. Fix via PharaohsVaultTask's diagnostic
        // fallback if wrong.
        public const string OpenMilestonesBtn = Root + "/helpCanvas/actionButtons/milestones";

        // Spins the slot machine. Shows both a Noble Token cost and a Pharaoh Token cost side by
        // side (interchangeable per the wiki) - confirmed by the user that the game always draws
        // from the free Noble Tokens (10/day) first and this button simply becomes non-clickable
        // once they're exhausted, never silently spending Pharaoh Tokens (bought currency). Safe to
        // click purely via IsClickable() like everywhere else in this codebase.
        public const string SpinBtn = Root + "/helpCanvas/playButton";

        // Cycles the spin's bet multiplier (shows the current value in its "text" child - exact
        // label format, e.g. "x10" vs "10", not verified live). Per the user: always use the
        // biggest available multiplier, since it's a proportional bet/payout (same expected coins
        // per token spent, fewer clicks).
        public const string ChangeBetBtn = Root + "/helpCanvas/bottomRightUI/changePlayQuantity";

        public const string BetQuantityTxt = ChangeBetBtn + "/text";
    }

    // Opened by ScarabGameLoc.OpenVaultBtn. Spends accumulated Ancient Coins (5000 per the wiki) for
    // 5 random rewards - no real choice involved, openButton is a safe no-op via IsClickable() if
    // not yet affordable. Live-confirmed, 2026-09-18: it's a "menus/" screen, not a "popups/" one -
    // the originally assumed "popups/PharaohsVault" never resolved at all (same wrong-guess pattern
    // already seen on BattlePass and TavernMarket), while a generic Watchdog sweep found
    // "menus/PharaohsVault/closeButton" resolving fine.
    public static class PharaohsVaultLoc
    {
        private const string Root = MenusLoc.Root + "/menus/PharaohsVault";

        public const string CloseBtn = Root + "/closeButton";

        public const string OpenBtn = Root + "/openButton";

        // Same bet-multiplier pattern as ScarabGameLoc.ChangeBetBtn above - always use the biggest
        // available (proportional cost/reward, fewer clicks to spend the same coin budget).
        public const string ChangeQuantityBtn = Root + "/bottomRightUI/changeQuantity";

        public const string QuantityTxt = ChangeQuantityBtn + "/text";
    }

    // Live-confirmed, 2026-09-18: this is a popup, not a menu (same wrong-guess pattern already seen
    // on BattlePass/TavernMarket/PharaohsVault) - a generic active-screen dump found it listed under
    // "popups" (with "menus/ScarabGame" still active underneath it, confirming it's an overlay).
    public static class ScarabGameShopLoc
    {
        private const string Root = MenusLoc.Root + "/popups/ScarabGameShop";

        public const string CloseBtn = Root + "/closeButton";

        // The Shop also has a "Monthly pass" tab with its own free claim ("Pharaoh's token x1"), not
        // yet handled - only the "sale" tab's free item is claimed today.
        public const string SaleTabBtn = Root + "/bg/submenuButtons/sale/button";

        public static class FreeTokenLoc
        {
            private const string ItemRoot = Root + "/bg/submenus/sale/items/scarabGameShopFreeTokenInteraction";

            // Named "purchaseButton" but confirmed genuinely free - sibling "freeText" label
            // ("Gratis"), same distinguishing pattern already used for Task 3's mystery box.
            public const string ClaimBtn = ItemRoot + "/claimBg/purchaseButton";
        }
    }

    // A Scarab-level XP reward track (per the user's screenshot, 2026-09-20: numbered tiers 9-16+,
    // a progress bar toward the next one, checkmarks on already-claimed tiers, a single "Claim"
    // button under the currently-reached one, locks on future ones) - folded into PharaohsVaultTask
    // per the user's request, since it's reached from the same Scarab's Game screen.
    public static class ScarabGameMilestonesLoc
    {
        // Live-confirmed, 2026-09-20: the popup root is real (a diagnostic dump of "popups" showed it
        // active right after ScarabGameLoc.OpenMilestonesBtn was clicked) but the literal object name
        // is "ScarabGameMileStones" (capital S in "Stones") - the original guess ("Milestones", per
        // ordinary English casing) silently never resolved since Unity's Transform.Find is
        // case-sensitive, so IsVisible always read false even though the popup was genuinely open on
        // screen - this is why the claim button was never even attempted (the task's own "not open"
        // branch always fired instead).
        public const string Root = MenusLoc.Root + "/popups/ScarabGameMileStones";

        // Live-confirmed, 2026-09-20: nested under "bg", not a direct Root child as first guessed -
        // real content is Root/bg/{nameBg,closeButton,Top-Info,rewardsList}.
        public const string CloseBtn = Root + "/bg/closeButton";

        // Fully live-confirmed, 2026-09-20, across several rounds of diagnostics. Real structure:
        // RewardsListRoot/Scroll View/Viewport/Content/milestones has 23 children - 20 properly
        // indexed tier nodes ("ScarabMilestone", "ScarabMilestone (1)".."(19)", no duplicate-name
        // issue here unlike Pirate's Prize) plus 3 decorative siblings
        // (MilestoneProgressBarBg/Starting/Ending, filtered out by name in
        // PharaohsVaultTask/ScarabGameMilestones). Viewport also has two off-screen scroll-hint
        // arrows (missingClaimsButtonLeft/Right) as siblings of Content, not part of the tier list.
        public const string RewardsListRoot = Root + "/bg/rewardsList";

        public const string MilestonesRoot = RewardsListRoot + "/Scroll View/Viewport/Content/milestones";

        // Each tier node has glow/currencyReward/claimedOverlay/locked/claimButton - exactly one of
        // claimedOverlay/locked/claimButton is active depending on that tier's state (already
        // claimed / not yet reached / ready to claim now).
        public static string ClaimBtn(string tierName) => $"{MilestonesRoot}/{tierName}/rewardRoot/claimButton";
    }
}
