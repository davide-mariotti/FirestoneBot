using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Firebot.Utilities;

namespace Firebot.GameModel.Features.Guild.Shop;

public static class FreePickaxes
{
    public static string QuantityText =>
        new GameText(Paths.MenusLoc.GuildShopLoc.FreePickaxeLoc.QuantityTxt).GetParsedText();

    public static int Quantity => StringUtils.TryParseIntFromString(QuantityText, out var val) ? val : 0;

    public static IEnumerator Claim => new GameButton(Paths.MenusLoc.GuildShopLoc.FreePickaxeLoc.ClaimBtn).Click();
}
