using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town;

public static class ExoticMerchant
{
    public static IEnumerator OpenSellItemsTab => new GameButton(Paths.ExoticMerchantLoc.SellItemsTabBtn).Click();

    public static IEnumerator OpenUpgradesTab => new GameButton(Paths.ExoticMerchantLoc.UpgradesTabBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.ExoticMerchantLoc.CloseBtn).Click();

    public static GameElement SellProductGrid => new(Paths.ExoticMerchantLoc.SellLoc.ProductGridRoot);

    public static GameElement UpgradesList => new(Paths.ExoticMerchantLoc.UpgradesLoc.UpgradesListRoot);

    public static IEnumerator NextUpgradeTree =>
        new GameButton(Paths.ExoticMerchantLoc.UpgradesLoc.NextTreeBtn).Click();
}
