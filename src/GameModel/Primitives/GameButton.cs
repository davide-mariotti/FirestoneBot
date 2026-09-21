using System;
using System.Collections;
using Firebot.GameModel.Base;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Firebot.Core.BotSettings;

namespace Firebot.GameModel.Primitives;

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
    ///     Confirmed live, 2026-09-17: some dynamically-instantiated list items (Inventory's chest
    ///     slots - pooled ScrollView cells, same pattern as Path of Glory's reward track) don't wire
    ///     their interaction to Button.onClick at all - the Button component exists and is
    ///     enabled/interactable, but onClick has zero listeners, so Click()'s onClick.Invoke() is a
    ///     silent no-op even though a real mouse click on the same element works fine. Their actual
    ///     handler reacts to Unity's pointer events directly (a component EventSystem would normally
    ///     drive via IPointerDownHandler/IPointerUpHandler/IPointerClickHandler), so this replays that
    ///     same event sequence straight at the target GameObject via ExecuteEvents. This never touches
    ///     the OS mouse/cursor or any real input device - it's a purely in-process call into this
    ///     game instance's own Unity event system, so it's safe to run unattended across many
    ///     concurrent game instances without one interfering with another or with the real mouse.
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
            var gameObject = target.gameObject;
            var pointerData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                pointerPress = gameObject
            };

            ExecuteEvents.Execute(gameObject, pointerData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(gameObject, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(gameObject, pointerData, ExecuteEvents.pointerClickHandler);
        }
        catch (Exception e)
        {
            Debug($"[FAILED] ClickSimulated threw exception: {e.Message}. Path: {Path}");
        }

        yield return new WaitForSeconds(InteractionDelay);
    }
}
