using System.Collections;
using Firebot.GameModel.Base;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     The battle HUD comes in alternate prefab variants ("...New", "Mobile", "Desktop", plain), and
///     which one is populated changes per client and even within a session. Every HUD button is
///     therefore mapped once per known variant, and these helpers pick whichever is live right now.
/// </summary>
public static class UiVariantButton
{
    /// <summary>Clicks the first visible candidate; the last one otherwise, so its debug log still fires.</summary>
    public static IEnumerator Click(params GameButton[] candidates)
    {
        foreach (var candidate in candidates)
            if (candidate.IsVisible())
                return candidate.Click();

        return candidates[^1].Click();
    }

    public static bool AnyVisible(params GameElement[] candidates)
    {
        foreach (var candidate in candidates)
            if (candidate != null && candidate.IsVisible())
                return true;

        return false;
    }
}
