using Firebot.GameModel.Base;
using UnityEngine.UI;

namespace Firebot.GameModel.Primitives;

/// <summary>
///     The sprite on an Image: what a price is paid in, next to its cost ("meteorite64",
///     "strangeDust64", "gem64"). Buttons that spend something check it before the click, so a price
///     that changes currency is never paid by mistake.
/// </summary>
public static class IconSprite
{
    public static string NameAt(string path)
    {
        var icon = GameElement.FindTransform(path);
        var image = icon == null ? null : icon.GetComponent<Image>();
        return image == null || image.sprite == null ? "" : image.sprite.name;
    }
}
