using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Library.FirestoneResearch;

/// <summary>The popup a Firestone Research node click opens.</summary>
public static class Preview
{
    public static string Name => new GameText(Paths.MenusLoc.FirestoneResearchPreviewLoc.NameTxt).GetParsedText();

    public static bool IsUnlocked => new GameText(Paths.MenusLoc.FirestoneResearchPreviewLoc.UnlockedTxt).IsVisible();

    public static bool IsMaxed => new GameText(Paths.MenusLoc.FirestoneResearchPreviewLoc.MaxedTxt).IsVisible();

    // "Level 5/30" -> 5; -1 when unreadable, so it never passes for an untouched node.
    public static int CurrentLevel =>
        new GameText(Paths.MenusLoc.FirestoneResearchPreviewLoc.LevelTxt).GetParsedFirstInt(-1);

    public static IEnumerator Start => new GameButton(Paths.MenusLoc.FirestoneResearchPreviewLoc.ActivateBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.FirestoneResearchPreviewLoc.CloseBtn).Click();
}
