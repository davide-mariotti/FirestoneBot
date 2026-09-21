namespace Firebot.Infrastructure;

/// <summary>
///     The 4 "flying bonus" objects that periodically cross the battle screen - confirmed real via a
///     static UnityPy asset scan (the old, pre-rewrite bot config had a stale [flying_bonus_hunter]
///     section referencing this exact feature, see git history/TESTING.md), then located live,
///     2026-09-20, via a scene-wide recursive name search: NOT under battleCanvas/SafeArea like every
///     other battle-screen path in this file - they're direct children of battleCanvas itself.
///     CoworkerMeteoriteHunter was found alongside the 3 originally-documented ones
///     (DragonWithBeer/FemaleDragonWithBeer/MeteoriteHunter) - a 4th variant, not previously known.
///     Each top-level container IS its own clickable Button (confirmed live: real Button component,
///     interactable=True while actually spawned/flying) - no special collider/physics click needed,
///     unlike Store's HUD button investigated earlier the same session. Only visible while actually
///     flying across the screen; IsClickable() naturally reads false the rest of the time (safe no-op,
///     same pattern used everywhere else in this codebase).
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
    }
}
