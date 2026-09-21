using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Library.MeteoriteResearch;

public static class MeteoriteResearchPreview
{
    public static string Name => new GameText(Paths.MenusLoc.MeteoriteResearchPreviewLoc.NameTxt).GetParsedText();

    public static bool IsUnlocked => new GameElement(Paths.MenusLoc.MeteoriteResearchPreviewLoc.UnlockedRoot).IsVisible();

    // 0 both when parsing fails and when the field is genuinely absent (e.g. a maxed-out node might
    // hide the research button entirely - unverified, no prior precedent) - either way, not a valid
    // candidate to research.
    public static double Cost => new GameText(Paths.MenusLoc.MeteoriteResearchPreviewLoc.CostTxt).GetParsedDoubleAbbreviated();

    public static IEnumerator Research => new GameButton(Paths.MenusLoc.MeteoriteResearchPreviewLoc.ResearchBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.MeteoriteResearchPreviewLoc.CloseBtn).Click();
}
