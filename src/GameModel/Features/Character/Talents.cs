using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Character;

/// <summary>
///     Reads and clicks the live Talents tab. The tree's shape is in TalentTreeData, and what to
///     invest in is decided by TalentEngine.TalentAllocator.
/// </summary>
public static class Talents
{
    // The counter reads "available/total", where total is every point ever earned.
    public static int AvailablePoints => new GameText(Paths.TalentsLoc.PointsLeftTxt).GetParsedLeadingInt();

    public static int TotalPointsAwarded => new GameText(Paths.TalentsLoc.PointsLeftTxt).GetParsedTrailingInt();

    public static IEnumerator Save => new GameButton(Paths.TalentsLoc.SaveBtn).Click();

    public static IEnumerator OpenNode(int catalogIndex) =>
        new GameButton($"{Paths.TalentsLoc.NodeRoot}/talentInteraction ({catalogIndex})").Click();

    public static IEnumerator ClosePreview => new GameButton(Paths.TalentPreviewLoc.CloseBtn).Click();

    /// <summary>The open preview is locked by the game's own per-node prerequisite.</summary>
    public static bool IsPreviewLocked => new GameElement(Paths.TalentPreviewLoc.LockedRoot).IsVisible();

    public static GameButton UpgradeButton => new(Paths.TalentPreviewLoc.UpgradeBtn);

    // "Level N/M" - the leading label is why this takes the first number, not a split on '/'.
    public static int PreviewCurrentRank => new GameText(Paths.TalentPreviewLoc.LevelTxt).GetParsedFirstInt();
}
