using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Alchemist;

public static class Alchemist
{
    public static IEnumerator Close => new GameButton(Paths.MenusLoc.AlchemistLoc.CloseBtn).Click();
}
