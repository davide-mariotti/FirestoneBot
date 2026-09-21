using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.TempleOfEternals;

public static class TempleOfEternals
{
    public static IEnumerator OpenEmpowerPopup =>
        new GameButton(Paths.MenusLoc.TempleOfEternalsLoc.EmpowerBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.TempleOfEternalsLoc.CloseBtn).Click();

    public static string AdventureTimePlayedText =>
        new GameText(Paths.MenusLoc.TempleOfEternalsLoc.AdventureTimePlayedTxt).GetParsedText();

    public static double FirestonesFound =>
        new GameText(Paths.MenusLoc.TempleOfEternalsLoc.FirestonesFoundTxt).GetParsedDoubleAbbreviated();

    public static double FirestonesYouOwn =>
        new GameText(Paths.MenusLoc.TempleOfEternalsLoc.FirestonesYouOwnTxt).GetParsedDoubleAbbreviated();
}
