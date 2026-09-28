using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Library.MeteoriteResearch;

/// <summary>The popup a Meteorite Research node click opens.</summary>
public static class MeteoriteResearchPreview
{
    public static string Name => new GameText(Paths.MenusLoc.MeteoriteResearchPreviewLoc.NameTxt).GetParsedText();

    public static bool IsUnlocked => new GameElement(Paths.MenusLoc.MeteoriteResearchPreviewLoc.UnlockedRoot).IsVisible();

    // 0 both when it can't be parsed and when there's no cost shown - either way, not a candidate.
    public static double Cost => new GameText(Paths.MenusLoc.MeteoriteResearchPreviewLoc.CostTxt).GetParsedDoubleAbbreviated();

    public static IEnumerator Research => new GameButton(Paths.MenusLoc.MeteoriteResearchPreviewLoc.ResearchBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.MeteoriteResearchPreviewLoc.CloseBtn).Click();
}
