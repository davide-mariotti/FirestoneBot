using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild.Shop;

public static class GuildShop
{
    public static IEnumerator OpenSuppliesTab => new GameButton(Paths.MenusLoc.GuildShopLoc.SuppliesTabBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.GuildShopLoc.CloseBtn).Click();
}
