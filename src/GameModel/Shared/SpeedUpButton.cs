using System;
using Firebot.Core;
using Firebot.GameModel.Base;
using Il2CppTMPro;
using UnityEngine.UI;

namespace Firebot.GameModel.Shared;

/// <summary>
///     The speed-up button of research slots, experiments and map missions (SpeedUpButtonInteraction:
///     finishDesc, costText, currencyIcon). In the free window of the last minutes it shows costText
///     'Free' with finishDesc and the gem icon hidden (seen live on 30/09); otherwise it costs gems.
/// </summary>
public static class SpeedUpButton
{
    /// <summary>No price on it: the gem icon is hidden. The one check that keeps a speed-up free.</summary>
    public static bool IsFree(string buttonPath)
    {
        var icon = GameElement.FindTransform(buttonPath)?.Find("currencyIcon");
        return icon != null && !icon.gameObject.activeInHierarchy;
    }

    /// <summary>
    ///     Some slot under `slotsRoot` shows a free speed-up or its claim button. Quiet, for polling:
    ///     the slots' buttons refresh on the game's slot timer a moment after the screen shows, and read
    ///     at once they still had the gem price inside the free window (Steam-27, 27/11: an experiment
    ///     at 2m50s and 44 s from the end, a research at 44 s; the fixed 3 s wait before hid it).
    /// </summary>
    public static bool AnySlotReady(string slotsRoot, string slotPrefix, string speedUpPath, string claimPath)
    {
        var root = GameElement.FindTransform(slotsRoot);
        if (root == null) return false;
        for (var i = 0; i < root.childCount; i++)
        {
            var slot = root.GetChild(i);
            if (!slot.gameObject.activeInHierarchy || !slot.name.StartsWith(slotPrefix)) continue;

            var claim = slot.Find(claimPath.TrimStart('/'));
            if (claim != null && claim.gameObject.activeInHierarchy) return true;

            var speedUp = slot.Find(speedUpPath.TrimStart('/'));
            var icon = speedUp == null ? null : speedUp.Find("currencyIcon");
            if (speedUp != null && speedUp.gameObject.activeInHierarchy && icon != null && !icon.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     When a countdown ending at end enters the free window. DateTime.MinValue (the text showed no
    ///     countdown, e.g. a slot started a moment ago) stays MinValue: subtracting from it throws.
    /// </summary>
    public static DateTime FreeFrom(DateTime end) =>
        end == DateTime.MinValue ? end : end.AddSeconds(-BotSettings.FreeSpeedupSeconds);

    public static string Describe(string buttonPath)
    {
        var button = GameElement.FindTransform(buttonPath);
        if (button == null) return "(no button)";

        string Text(string child)
        {
            var t = button.Find(child);
            if (t == null) return "(none)";
            var tmp = t.GetComponent<TMP_Text>();
            return (t.gameObject.activeInHierarchy ? "" : "hidden ") + (tmp == null ? "" : $"'{tmp.text}'");
        }

        var icon = button.Find("currencyIcon");
        var image = icon == null ? null : icon.GetComponent<Image>();
        var iconName = image == null || image.sprite == null ? "(none)" : image.sprite.name;
        if (icon != null && !icon.gameObject.activeInHierarchy) iconName = "hidden " + iconName;

        return $"finishDesc {Text("finishDesc")}, costText {Text("costText")}, currencyIcon {iconName}";
    }
}
