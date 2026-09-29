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

    // The raw texts, for the log: both counts read as 0 on every run of the 26-29/09 logs.
    public static string FirestonesFoundText =>
        new GameText(Paths.MenusLoc.TempleOfEternalsLoc.FirestonesFoundTxt).GetParsedText();

    public static string FirestonesYouOwnText =>
        new GameText(Paths.MenusLoc.TempleOfEternalsLoc.FirestonesYouOwnTxt).GetParsedText();
}
