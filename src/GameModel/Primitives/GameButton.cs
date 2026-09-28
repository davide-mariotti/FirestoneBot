using System;
using System.Collections;
using Firebot.GameModel.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Firebot.Core.BotSettings;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     A clickable element. Every click is a safe no-op when the button is hidden, disabled or not
///     interactable, which is how the rest of the code handles "nothing to do here" without checks.
/// </summary>
public class GameButton : GameElement
{
    public GameButton(string path = null, GameElement parent = null, Transform transform = null) :
        base(path, parent, transform) { }

    public bool IsClickable() => IsClickable(out _);

    private bool IsClickable(out Button button)
    {
        button = null;
        if (!IsVisible()) return false;

        if (!TryGetComponent(out button)) return false;
        return button.enabled && button.interactable;
    }

    public virtual IEnumerator Click()
    {
        if (IsClickable(out var button))
            try
            {
                button.onClick.Invoke();
            }
            catch (Exception e)
            {
                Debug($"[FAILED] Click threw exception: {e.Message}. Path: {Path}");
            }
        else
        {
            if (button != null)
                Debug($"[FAILED] Click ignored: Button disabled/non-interactable. Path: {Path}");
        }

        yield return new WaitForSeconds(InteractionDelay);
    }

    /// <summary>
    ///     For UI whose Button.onClick has no listeners (pooled list cells, some HUD buttons): the
    ///     real handler reacts to pointer events, so Click() is a silent no-op there. This replays
    ///     pointer down/up/click through this game instance's own EventSystem - it never touches the
    ///     OS mouse, so it's safe across many concurrent instances.
    /// </summary>
    public virtual IEnumerator ClickSimulated()
    {
        var target = Root;
        if (target == null || !IsVisible())
        {
            Debug($"[FAILED] ClickSimulated skipped: element not visible. Path: {Path}");
            yield return new WaitForSeconds(InteractionDelay);
            yield break;
        }

        if (EventSystem.current == null)
        {
            Debug($"[FAILED] ClickSimulated skipped: no EventSystem in scene. Path: {Path}");
            yield return new WaitForSeconds(InteractionDelay);
            yield break;
        }

        try
        {
            DispatchPointerClick(target.gameObject);
        }
        catch (Exception e)
        {
            Debug($"[FAILED] ClickSimulated threw exception: {e.Message}. Path: {Path}");
        }

        yield return new WaitForSeconds(InteractionDelay);
    }

    /// <summary>
    ///     The pointer event sequence behind ClickSimulated, for callers holding a raw GameObject.
    ///     Needs EventSystem.current, which callers check first.
    /// </summary>
    internal static void DispatchPointerClick(GameObject target)
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            pointerPress = target
        };

        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
    }
}
