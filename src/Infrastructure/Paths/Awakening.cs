namespace Firebot.Infrastructure;

/// <summary>Awakening: spends Arcane Crystals on a hero the game picks.</summary>
public static partial class Paths
{
    public static class AwakeningLoc
    {
        private const string Root = MenusLoc.Root + "/menus/Awakening";

        public const string CloseBtn = Root + "/closeButton";

        // One button per multiplier, hidden until the account unlocks it (04/10: x1/x2/x5 on Steam-0,
        // x1/x2 on Steam-9). A shown one stays clickable without the crystals; AwakenBtn doesn't.
        private const string OptionsRoot = Root + "/awakeningOptions";
        public const string QuantityBtn1 = OptionsRoot + "/awakeningQuantityGrid/changeQuantityButton1";
        public const string QuantityBtn2 = OptionsRoot + "/awakeningQuantityGrid/changeQuantityButton2";
        public const string QuantityBtn5 = OptionsRoot + "/awakeningQuantityGrid/changeQuantityButton5";
        public const string QuantityBtn10 = OptionsRoot + "/awakeningQuantityGrid/changeQuantityButton10";
        public const string QuantityBtn20 = OptionsRoot + "/awakeningQuantityGrid1/changeQuantityButton20";
        public const string QuantityBtn40 = OptionsRoot + "/awakeningQuantityGrid1/changeQuantityButton40";
        public const string QuantityBtn80 = OptionsRoot + "/awakeningQuantityGrid1/changeQuantityButton80";
        public const string QuantityBtn160 = OptionsRoot + "/awakeningQuantityGrid1/changeQuantityButton160";

        public const string AwakenBtn = OptionsRoot + "/awakenBg/awakenButton";
        public const string CostTxt = AwakenBtn + "/costText";

        // Shows 'Manual' or 'Auto'; hidden while the selected multiplier isn't covered.
        public const string AutoToggleBtn = Root + "/autoAwakenToggle";
        public const string BalanceTxt = Root + "/counters/currencyInteraction (ArcaneCrystal)/quantity";
    }
}
