namespace Firebot.Infrastructure;

/// <summary>
///     Awakening - spends Arcane Crystals to level up a randomly-chosen hero (the game itself picks,
///     weighted toward heroes with a lower awakening level per the wiki - no selection logic needed
///     here at all). Unlocks at level 50 per the wiki. Reached from Guild -&gt; TownGuildLoc.AwakeningBtn.
///     This whole feature is new, found via a targeted UnityPy scan requested by
///     the user.
/// </summary>
public static partial class Paths
{
    public static class AwakeningLoc
    {
        private const string Root = MenusLoc.Root + "/menus/Awakening";

        public const string CloseBtn = Root + "/closeButton";

        // One directly-named button per multiplier tier (not a single cycling button like
        // ScarabGame's bet selector) - confirmed via UnityPy, matches the wiki's multiplier table
        // exactly (x1 always available; x2/x5/x10/x20/x40/x80/x160 gated by hero awakening level +
        // crystal balance thresholds, assumed enforced by the button's own IsClickable() like every
        // other gated button in this codebase).
        private const string OptionsRoot = Root + "/awakeningOptions";
        public const string QuantityBtn1 = OptionsRoot + "/awakeningQuantityGrid/changeQuantityButton1";
        public const string QuantityBtn2 = OptionsRoot + "/awakeningQuantityGrid/changeQuantityButton2";
        public const string QuantityBtn5 = OptionsRoot + "/awakeningQuantityGrid/changeQuantityButton5";
        public const string QuantityBtn10 = OptionsRoot + "/awakeningQuantityGrid/changeQuantityButton10";
        public const string QuantityBtn20 = OptionsRoot + "/awakeningQuantityGrid1/changeQuantityButton20";
        public const string QuantityBtn40 = OptionsRoot + "/awakeningQuantityGrid1/changeQuantityButton40";
        public const string QuantityBtn80 = OptionsRoot + "/awakeningQuantityGrid1/changeQuantityButton80";
        public const string QuantityBtn160 = OptionsRoot + "/awakeningQuantityGrid1/changeQuantityButton160";

        // Performs the awaken at whichever multiplier is currently selected via the buttons above.
        public const string AwakenBtn = OptionsRoot + "/awakenBg/awakenButton";

        // NEVER used: toggles some kind of built-in auto-awaken mode (found right next to the close
        // button) - whether it persists across screen visits/game restarts, or interacts with the
        // multiplier selection, isn't known, and the explicit select-multiplier-then-click-awaken
        // flow above is already consistent with every other purchase loop in this codebase, so this
        // wasn't investigated further.
        public const string AutoAwakenToggleDoNotUse = Root + "/autoAwakenToggle";
    }
}
