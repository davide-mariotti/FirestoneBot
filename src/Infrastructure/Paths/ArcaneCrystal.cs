namespace Firebot.Infrastructure;

/// <summary>
///     The Guild's Arcane Crystal screen (a shared, guild-wide "boss" you chip away at with
///     pickaxes). Never automated before. Reached via TownGuildLoc.ArcaneCrystalBtn.
/// </summary>
public static partial class Paths
{
    public static class ArcaneCrystalLoc
    {
        private const string Root = MenusLoc.Root + "/menus/ArcaneCrystal";

        public const string CloseBtn = Root + "/closeButton";

        // Costs pickaxes per hit (confirmed via UnityPy: sits next to a pickaxeIcon) - exact cost
        // not statically known, read live via GameButton's own safe no-op if unaffordable.
        public const string HitBtn = Root + "/crystalParent/hitButton";

        // Cycles a hit-quantity multiplier (per the static scan, a common idle-RPG pattern like
        // x1/x10/x100 - exact values not live-confirmed). User-requested optimization: if "5" turns
        // out to be one of the options, MinerQuestTask can do its 5 required hits in a single click
        // instead of 5 - see ArcaneCrystal.TrySetQuantityTo5.
        public const string ChangeQuantityBtn = Root + "/changeHitQuantity";

        public const string QuantityTxt = ChangeQuantityBtn + "/text";
    }
}
