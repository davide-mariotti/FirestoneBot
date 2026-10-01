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
