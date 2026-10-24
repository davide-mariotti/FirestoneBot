using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Events;

/// <summary>Halloween's witch's hut. Only the Exchange is used - never the avatars or the paid Pumpkin Shop.</summary>
public static class TrickOrTreatShop
{
    public static bool IsVisible => new GameElement(Paths.TrickOrTreatShopLoc.Root).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.TrickOrTreatShopLoc.CloseBtn).Click();

    public static IEnumerator OpenExchangeTab => new GameButton(Paths.TrickOrTreatShopLoc.ExchangeTabBtn).Click();

    // The user's choice (24/10): legendary chests first, then meteorites. The gear chest is
    // "Legendary chest" only from level 130 ("Epic chest" on Steam-17), so lower accounts go
    // straight to meteorites. No Beer: at 1,250 it would eat what a 1,500 meteorite pack waits for.
    public static readonly ExchangeTab Exchange = new("TrickOrTreatShop",
        Paths.TrickOrTreatShopLoc.ExchangeLoc.ChangeQuantityBtn, Paths.TrickOrTreatShopLoc.ExchangeLoc.ChangeQuantityTxt,
        Paths.TrickOrTreatShopLoc.ExchangeLoc.ItemsRoot, Paths.TrickOrTreatShopLoc.ExchangeLoc.ItemNameTxt,
        Paths.TrickOrTreatShopLoc.ExchangeLoc.ItemBuyBtn, new[] { "Legendary chest", "Meteorite" });
}
