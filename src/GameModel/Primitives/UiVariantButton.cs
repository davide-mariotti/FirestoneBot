using System.Collections;
using Firebot.GameModel.Base;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     Several battle-screen HUD regions have two live prefab variants (informally "New" vs
///     "normal") that the game picks between - confirmed live, 2026-09-17: a same-session,
///     same-timing diagnostic dump of every direct child of SafeArea showed "leftSideUINew"
///     populated with real content (mail, notifications, chat, ...) while "leftSideUI" had a
///     single unrelated child, then a same-timing retest right after found the exact opposite -
///     strongly suggesting the choice is made per client (most likely screen resolution/aspect
///     ratio) rather than being fixed for a given game version. Hard-coding either one alone works
///     in one session and silently does nothing in another.
/// </summary>
public static class UiVariantButton
{
    /// <summary>
    ///     Clicks the first candidate that's actually visible this session. If none are (e.g. a
    ///     genuinely-absent badge, not a variant mismatch), clicks the last one anyway so the
    ///     normal "hidden"/"not clickable" debug logging still fires instead of silently doing
    ///     nothing.
    /// </summary>
    public static IEnumerator Click(params GameButton[] candidates)
    {
        foreach (var candidate in candidates)
            if (candidate.IsVisible())
                return candidate.Click();

        return candidates[^1].Click();
    }

    /// <summary>Whether any candidate resolves and is currently active - used for scheduling checks (see BotTask.IsNotificationVisible), not clicking.</summary>
    public static bool AnyVisible(params GameElement[] candidates)
    {
        foreach (var candidate in candidates)
            if (candidate != null && candidate.IsVisible())
                return true;

        return false;
    }
}
