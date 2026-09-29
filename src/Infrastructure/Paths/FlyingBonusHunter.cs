namespace Firebot.Infrastructure;

/// <summary>
///     The bonuses that fly across the battle screen now and then. They're direct children of
///     battleCanvas (not SafeArea like the rest of the HUD), each its own Button. The Buttons are
///     always active and interactable; only their single child is active, and only while flying.
/// </summary>
public static partial class Paths
{
    public static class FlyingBonusHunterLoc
    {
        private const string Root = "battleRoot/battleMain/battleCanvas";

        public const string MeteoriteHunterBtn = Root + "/MeteoriteHunter";
        public const string CoworkerMeteoriteHunterBtn = Root + "/CoworkerMeteoriteHunter";
        public const string DragonWithBeerBtn = Root + "/DragonWithBeer";
        public const string FemaleDragonWithBeerBtn = Root + "/FemaleDragonWithBeer";

        // Active only while the bonus is on screen.
        public const string MeteoriteHunterFlying = MeteoriteHunterBtn + "/hunter";
        public const string CoworkerMeteoriteHunterFlying = CoworkerMeteoriteHunterBtn + "/hunter";
        public const string DragonWithBeerFlying = DragonWithBeerBtn + "/dragon";
        public const string FemaleDragonWithBeerFlying = FemaleDragonWithBeerBtn + "/femaleDragon";
    }
}
