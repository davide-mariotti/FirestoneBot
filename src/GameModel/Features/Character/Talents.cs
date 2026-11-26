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

    public static IEnumerator OpenNode(int catalogIndex) => new GameButton(NodePath(catalogIndex)).Click();

    /// <summary>The game's data behind a node (level, levelCap, unlocked); null when the node isn't there.</summary>
    public static Il2Cpp.Talents NodeData(int catalogIndex) =>
        new GameElement(NodePath(catalogIndex)).TryGetComponent<Il2Cpp.TalentsInteraction>(out var node) ? node.talent : null;

    private static string NodePath(int catalogIndex) => $"{Paths.TalentsLoc.NodeRoot}/talentInteraction ({catalogIndex})";

    public static IEnumerator ClosePreview => new GameButton(Paths.TalentPreviewLoc.CloseBtn).Click();

    public static GameButton UpgradeButton => new(Paths.TalentPreviewLoc.UpgradeBtn);
}
