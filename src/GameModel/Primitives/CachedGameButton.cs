using System.Collections;
using Firebot.GameModel.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     A GameButton that resolves its Transform once and keeps it, for a background loop that polls
///     the same button every few seconds. Only pays off when the same instance is reused.
/// </summary>
public class CachedGameButton : GameButton
{
    private Transform _cachedRoot;

    public CachedGameButton(string path = null, GameElement parent = null, Transform transform = null)
        : base(path, parent, transform) { }

    // Unity's overloaded != treats a destroyed Transform as null, so a scene reload re-resolves it.
    private Transform CachedRoot
    {
        get
        {
            if (_cachedRoot != null) return _cachedRoot;

            _cachedRoot = Root;
            return _cachedRoot;
        }
    }

    private bool IsClickableCached(out Button button)
    {
        button = null;
        var currentRoot = CachedRoot;
        if (currentRoot == null || !currentRoot.gameObject.activeInHierarchy) return false;

        if (!currentRoot.TryGetComponent(out button)) return false;
        return button.enabled && button.interactable;
    }

    /// <summary>Presses the button and holds it until it stops being interactable or maxSeconds pass.</summary>
    public IEnumerator HoldButton(float maxSeconds = 3f)
    {
        if (!IsClickableCached(out var button))
            yield break;

        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug($"[FAILED] HoldButton requires EventSystem.current. Path: {Path}");
            yield break;
        }

        var eventData = new PointerEventData(eventSystem);

        ExecuteEvents.Execute(button.gameObject, eventData, ExecuteEvents.pointerDownHandler);

        var startTime = Time.time;
        while (button != null && button.enabled && button.interactable && Time.time - startTime < maxSeconds)
            yield return null;
    }
}
