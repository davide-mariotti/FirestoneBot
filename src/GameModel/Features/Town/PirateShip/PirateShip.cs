using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.PirateShip;

public static class PirateShip
{
    public static IEnumerator Close => new GameButton(Paths.PirateShipLoc.CloseBtn).Click();
}
