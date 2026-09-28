namespace Firebot.Infrastructure;

/// <summary>Awakening: spends Arcane Crystals on a hero the game picks.</summary>
public static partial class Paths
{
    public static class AwakeningLoc
    {
        private const string Root = MenusLoc.Root + "/menus/Awakening";

        public const string CloseBtn = Root + "/closeButton";

        // One button per multiplier. x1 is always available; the rest unlock with hero awakening
        // level and crystal balance, which each button's own clickable state reflects.
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
    }
}
