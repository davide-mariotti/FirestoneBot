using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Shared;

public static class PlayerAvatar
{
    private static int? _cachedLevel;

    // Every task's readiness check reads the level, so it's cached for one scheduler tick
    // (RefreshCachedLevel) instead of being re-parsed from the UI 2-3 times per task per tick.
    public static int CharacterLevel => _cachedLevel ??= ReadCharacterLevel();

    public static void RefreshCachedLevel() => _cachedLevel = ReadCharacterLevel();

    private static int ReadCharacterLevel() =>
        new GameText(Paths.BattleLoc.PlayerAvatarLoc.CharacterLevel).GetParsedInt();
}
