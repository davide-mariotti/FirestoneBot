using System;
using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Firebot.Utilities;

namespace Firebot.GameModel.Features.Town.Library.FirestoneResearch;

public static class Preview
{
    public static string Name => new GameText(Paths.MenusLoc.FirestoneResearchPreviewLoc.NameTxt).GetParsedText();

    public static bool IsUnlocked => new GameText(Paths.MenusLoc.FirestoneResearchPreviewLoc.UnlockedTxt).IsVisible();

    public static bool IsMaxed => new GameText(Paths.MenusLoc.FirestoneResearchPreviewLoc.MaxedTxt).IsVisible();

    public static int CurrentLevel
    {
        get
        {
            var text = new GameText(Paths.MenusLoc.FirestoneResearchPreviewLoc.LevelTxt).GetParsedText();
            return StringUtils.TryParseIntFromString(text, out var level) ? level : 0;
        }
    }

    public static TimeSpan TimeRequired =>
        TimeParser.ParseFrom(new GameText(Paths.MenusLoc.FirestoneResearchPreviewLoc.RealTimeTxt).GetParsedText());

    public static IEnumerator Start => new GameButton(Paths.MenusLoc.FirestoneResearchPreviewLoc.ActivateBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.FirestoneResearchPreviewLoc.CloseBtn).Click();
}
