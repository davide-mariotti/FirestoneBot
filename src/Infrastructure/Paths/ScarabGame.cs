namespace Firebot.Infrastructure;

/// <summary>Scarab's Game (a slot machine), its shop, Pharaoh's Vault and the milestone track.</summary>
public static partial class Paths
{
    public static class ScarabGameLoc
    {
        private const string Root = MenusLoc.Root + "/menus/ScarabGame";

        public const string CloseBtn = Root + "/closeButton";

        // Lit once the six sigils are found; inactive otherwise (Steam-0, 30/09).
        public const string ReleaseBeastBtn = Root + "/secondaryObjects/tablet/releaseBeast/releaseButton";

        // The card the release shows. From the asset dump, not seen live yet.
        public const string ReleasedBeastCloseBtn = Root + "/releaseTheBeastPopup/closeButton";

        public const string OpenBeastsBtn = Root + "/helpCanvas/actionButtons/beasts";

        public const string OpenShopBtn = Root + "/helpCanvas/actionButtons/shop";

        public const string OpenVaultBtn = Root + "/helpCanvas/actionButtons/pharaohVault";

        // Spends the free Noble Tokens (10/day) first and turns unclickable once they run out - it
        // never falls back to Pharaoh Tokens, which are bought.
        public const string SpinBtn = Root + "/helpCanvas/playButton";

        public const string ChangeBetBtn = Root + "/helpCanvas/bottomRightUI/changePlayQuantity";

        public const string BetQuantityTxt = ChangeBetBtn + "/text";
    }

    // Spends Ancient Coins (5000 per open) for 5 random rewards.
    public static class PharaohsVaultLoc
    {
        private const string Root = MenusLoc.Root + "/menus/PharaohsVault";

        public const string CloseBtn = Root + "/closeButton";

        public const string OpenBtn = Root + "/openButton";

        public const string ChangeQuantityBtn = Root + "/bottomRightUI/changeQuantity";

        public const string QuantityTxt = ChangeQuantityBtn + "/text";
    }

    public static class ScarabGameShopLoc
    {
        private const string Root = MenusLoc.Root + "/popups/ScarabGameShop";

        public const string CloseBtn = Root + "/closeButton";

        public const string SaleTabBtn = Root + "/bg/submenuButtons/sale/button";

        public static class FreeTokenLoc
        {
            private const string ItemRoot = Root + "/bg/submenus/sale/items/scarabGameShopFreeTokenInteraction";

            // Named "purchaseButton" but free (its label reads "free").
            public const string ClaimBtn = ItemRoot + "/claimBg/purchaseButton";
        }
    }

    public static class ScarabGameMilestonesLoc
    {
        // "MileStones" with a capital S is the game's own spelling, and Transform.Find is case-sensitive.
        public const string Root = MenusLoc.Root + "/popups/ScarabGameMileStones";

        public const string CloseBtn = Root + "/bg/closeButton";

        private const string RewardsListRoot = Root + "/bg/rewardsList";

        // 20 tiers "ScarabMilestone", "ScarabMilestone (1)".."(19)" plus 3 decorative siblings.
        public const string MilestonesRoot = RewardsListRoot + "/Scroll View/Viewport/Content/milestones";

        // Per tier, exactly one of claimedOverlay / locked / claimButton is active.
        public static string ClaimBtn(string tierName) => $"{MilestonesRoot}/{tierName}/rewardRoot/claimButton";
    }

    // The beast collection, from the Scarab's Game's "beasts" button. Seen live on Steam-0 (30/09).
    public static class BeastsLoc
    {
        private const string Root = MenusLoc.Root + "/menus/Beasts";

        public const string CloseBtn = Root + "/closeButton";

        // "BeastAvatar (N)", one per beast owned.
        public const string AvatarsRoot = Root + "/beastList/beastScrollView/Viewport/Content";

        // Opens BeastModifyLoc as "Beast Upgrade" (Soul Embers). Its sibling openIncreaseRarityPopupButton
        // opens it as "Beast Rarity" (Cobra Keys), which the bot leaves alone.
        public const string OpenUpgradePopupBtn = Root + "/openUpgradePopupButton";

        // "1/30": owned of all.
        public const string BeastCountTxt = Root + "/counters/beastCounter/beastNum";
    }

    public static class BeastModifyLoc
    {
        private const string Root = MenusLoc.Root + "/popups/BeastModify/bg";

        public const string CloseBtn = Root + "/closeButton";

        // "Upgrade", 10 Soul Embers (icon soulEmber64) for level 1 -> 2 on Steam-0 (30/09).
        public const string ModifyBtn = Root + "/btnModify";

        public const string ModifyCostIcon = ModifyBtn + "/currencyIcon";

        public const string SoulEmbersTxt = Root + "/counters/currencyInteraction (SoulEmber)/quantity";
    }
}
