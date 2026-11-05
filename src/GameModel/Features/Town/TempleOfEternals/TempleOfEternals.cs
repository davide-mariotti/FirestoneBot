using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Firebot.Utilities;

namespace Firebot.GameModel.Features.Town.TempleOfEternals;

public static class TempleOfEternals
{
    public static IEnumerator OpenEmpowerPopup =>
        new GameButton(Paths.MenusLoc.TempleOfEternalsLoc.EmpowerBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.TempleOfEternalsLoc.CloseBtn).Click();

    // Checked silently: polled while the screen is still opening.
    public static bool IsShown =>
        GameElement.FindTransform(Paths.MenusLoc.TempleOfEternalsLoc.FirestonesYouOwnTxt) is { } t &&
        t.gameObject.activeInHierarchy;

    public static string AdventureTimePlayedText =>
        new GameText(Paths.MenusLoc.TempleOfEternalsLoc.AdventureTimePlayedTxt).GetParsedText();

    // log10: past 1.8e308 a double reads Infinity, and Infinity / Infinity held Steam-0's Empower back
    // for two days (03-05/11).
    public static double FirestonesFoundLog10 => StringUtils.ParseAbbreviatedLog10(FirestonesFoundText);

    public static double FirestonesYouOwnLog10 => StringUtils.ParseAbbreviatedLog10(FirestonesYouOwnText);

    // The raw texts, for the log: both counts read as 0 on every run of the 26-29/09 logs.
    public static string FirestonesFoundText =>
        new GameText(Paths.MenusLoc.TempleOfEternalsLoc.FirestonesFoundTxt).GetParsedText();

    public static string FirestonesYouOwnText =>
        new GameText(Paths.MenusLoc.TempleOfEternalsLoc.FirestonesYouOwnTxt).GetParsedText();
}
