namespace Firebot.Infrastructure;

/// <summary>
///     Chaos Rift (https://firestone-idle-rpg.fandom.com/wiki/Chaos_Rift), a server-wide shared boss
///     ("dark god" - Kramatak/Ledra/Yamanoth, rotating monthly) - unlocks at character level 100 per
///     the wiki infobox. Every attack costs 1 Moon Stone (free, caps at 10, recharges fully once a
///     day) or, once those run out, 1 Eclipse Stone (bought here with Dark Rune, the currency earned
///     from damage dealt). Reached from Town -&gt; Guild -&gt; TownGuildLoc.ChaosRiftBtn.
///     Paths cross-checked against a UnityPy dump (docs/screens/ChaosRift.html,
///     docs/screens/ChaosRiftShop.html) via a research pass, then fully live-confirmed across several
///     rounds of diagnostics, 2026-09-20 - the "menus/ChaosRift" root guess and closeButton were right
///     first try, but the interactive layer (hitButton/actionButtons/autoHitToggle) turned out nested
///     one level deeper than the dump's own top-level listing suggested - see ChaosRiftTask's history.
/// </summary>
public static partial class Paths
{
    public static class ChaosRiftLoc
    {
        public const string Root = MenusLoc.Root + "/menus/ChaosRift";

        public const string CloseBtn = Root + "/closeButton";

        // Live-confirmed, 2026-09-20: the interactive layer isn't directly under Root - Root's own
        // children are just the 3 realm background layers (ledraBg/yamanothBg/kramatakBg, shared with
        // ForbiddenKnowledgeLoc - see that file) plus this UIElements wrapper and closeButton.
        private const string UiRoot = Root + "/UIElements";

        // Live-confirmed, 2026-09-20: nested under godParent (bundled with the boss HP display, shadow
        // and hit VFX/animation children), not a direct UiRoot sibling of autoHitToggle/actionButtons
        // like the other two below.
        public const string HitBtn = UiRoot + "/godParent/hitButton";

        public const string AutoHitToggleBtn = UiRoot + "/autoHitToggle";

        // Live-confirmed present, 2026-09-23 (fresh UnityPy dump of UIElements' full sibling list) -
        // a direct sibling of autoHitToggle, same Button+text shape as ArcaneCrystal.ChangeQuantityBtn
        // / Tavern.ChangePlayQuantityBtn (a single cycling multiplier, not distinct buttons per value
        // like Awakening). The exact cycle values it shows aren't confirmed live yet - see
        // ChaosRift.TrySetQuantityTo.
        public const string ChangeHitQuantityBtn = UiRoot + "/changeHitQuantity";

        public const string HitQuantityTxt = ChangeHitQuantityBtn + "/text";

        public const string ShopBtn = UiRoot + "/actionButtons/shop";

        // Live-confirmed present, 2026-09-20 (a diagnostic dump of actionButtons' 3 children:
        // upgrades/competitionLeaderboards/shop) but never clicked into until now - per the user's
        // screenshot, this opens a "Chaos rift"-branded guardian Holy Damage upgrade screen (the
        // wiki's Orb of Light mechanic). See GuardianHolyUpgradeTask.
        public const string UpgradesBtn = UiRoot + "/actionButtons/upgrades";
    }

    public static class ChaosRiftShopLoc
    {
        public const string Root = MenusLoc.Root + "/menus/ChaosRiftShop";

        public const string CloseBtn = Root + "/closeButton";

        // Live-confirmed, 2026-09-20 (this whole sub-tree, including Root itself, only exists as a
        // real GameObject after the first successful "shop" click - see ChaosRiftShop.WaitUntilOpen).
        // Per the dump, "Supplies" is the only one of 4 tabs that spends the free Dark Rune currency -
        // "Sale" (likely the Monthly Pass), "Daily Deals" and "Weekly Deals" all use realCurrencyIcon
        // (real money per the wiki's own pricing note) and must never be touched.
        public const string SuppliesTabBtn = Root + "/bg/submenuButtons/supplies/button";

        private const string SuppliesRoot = Root + "/bg/submenus/supplies/items";

        // Per the wiki, Dark Runes' primary documented use is buying Tomes of Power (current god) -
        // tried first since it directly feeds ForbiddenKnowledgeTask.
        public const string TomeOfPowerBuyBtn = SuppliesRoot + "/tomeOfPower/claimBg/purchaseButton";

        // Live screenshot (user, 2026-09-19) shows these two displaying "Eclipse stone" bundles
        // rather than the literal "moonstoneOne"/"moonstoneBulk" object names found in the dump.
        // Explicit standing instruction from the user (2026-09-20): NEVER buy Eclipse Stone, only
        // Tome of Power - kept defined here (real, live-confirmed paths) but deliberately never
        // wired to any GameModel/task code, same convention as Awakening.AutoAwakenToggleDoNotUse.
        public const string MoonstoneOneBuyBtn = SuppliesRoot + "/moonstoneOne/claimBg/purchaseButton";
        public const string MoonstoneBulkBuyBtn = SuppliesRoot + "/moonstoneBulk/claimBg/purchaseButton";
    }
}
