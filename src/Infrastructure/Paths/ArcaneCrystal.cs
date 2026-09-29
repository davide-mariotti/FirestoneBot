namespace Firebot.Infrastructure;

/// <summary>The Guild's Arcane Crystal, hit with pickaxes.</summary>
public static partial class Paths
{
    public static class ArcaneCrystalLoc
    {
        private const string Root = MenusLoc.Root + "/menus/ArcaneCrystal";

        public const string CloseBtn = Root + "/closeButton";

        public const string HitBtn = Root + "/crystalParent/hitButton";

        // Pickaxes per click: "1" at x1.
        public const string HitCostTxt = HitBtn + "/costText";

        // Hidden on some accounts (Steam-15, 29/09), where every hit is a single one.
        public const string ChangeQuantityBtn = Root + "/changeHitQuantity";

        public const string QuantityTxt = ChangeQuantityBtn + "/text";

        public const string PickaxeCountTxt = Root + "/counters/currencyInteraction (Pickaxe)/quantity";
    }
}
