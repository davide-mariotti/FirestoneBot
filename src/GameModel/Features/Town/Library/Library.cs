using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Library;

public static class Library
{
    public static IEnumerator OpenFirestoneResearchTab =>
        new GameButton(Paths.MenusLoc.LibraryLoc.FirestoneResearchTabBtn).Click();

    public static IEnumerator OpenMeteoriteResearchTab =>
        new GameButton(Paths.MenusLoc.LibraryLoc.MeteoriteResearchTabBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.LibraryLoc.CloseBtn).Click();
}
