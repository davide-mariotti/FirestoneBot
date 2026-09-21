using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Shared;

public static class PlayerAvatar
{
    private static int? _cachedLevel;

    // Cached for one BotManager scan tick (see RefreshCachedLevel, called once per tick) - reading
    // this is an uncached Transform.Find + text parse, and every task's IsReady()/
    // IsNotificationVisible() check touches it, so an uncached read repeated it 2-3x per task per
    // tick across ~30 tasks. The character level changes at most a few times a day, so per-tick
    // freshness loses nothing.
    public static int CharacterLevel => _cachedLevel ??= ReadCharacterLevel();

    public static void RefreshCachedLevel() => _cachedLevel = ReadCharacterLevel();

    private static int ReadCharacterLevel() =>
        new GameText(Paths.BattleLoc.PlayerAvatarLoc.CharacterLevel).GetParsedInt();
}
