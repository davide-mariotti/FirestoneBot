using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Oracle;

public static class Oracle
{
    public static IEnumerator Close => new GameButton(Paths.MenusLoc.OracleLoc.CloseBtn).Click();

    public static IEnumerator OpenRituals => OpenTab(Paths.MenusLoc.OracleLoc.RitualsTabBtn);

    public static IEnumerator OpenStore => OpenTab(Paths.MenusLoc.OracleLoc.StoreTabBtn);

    // At 10 fps the Oracle shows up 1-2 s after the town click: the tab clicked at once was still hidden
    // (10/10, Steam-0).
    private static IEnumerator OpenTab(string path)
    {
        var tab = new GameButton(path);
        yield return Poll.Until(tab.IsVisible);
        yield return tab.Click();
    }
}
