namespace Firebot.Infrastructure;

/// <summary>The Guild's Arcane Crystal, hit with pickaxes.</summary>
public static partial class Paths
{
    public static class ArcaneCrystalLoc
    {
        private const string Root = MenusLoc.Root + "/menus/ArcaneCrystal";

        public const string CloseBtn = Root + "/closeButton";

        public const string HitBtn = Root + "/crystalParent/hitButton";

        public const string ChangeQuantityBtn = Root + "/changeHitQuantity";

        public const string QuantityTxt = ChangeQuantityBtn + "/text";
    }
}
