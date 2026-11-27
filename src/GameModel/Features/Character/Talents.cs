using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Character;

/// <summary>
///     Reads and clicks the live Talents tab. The tree's shape is in TalentTreeData, and what to
///     invest in is decided by TalentEngine.TalentAllocator.
/// </summary>
public static class Talents
{
    // The game's counts (TalentsHandler: usedTalentPoints was the counter's total - available on every run
    // of -27 and -28, 27/11); the counter "available/total" only when the game can't be read.
    public static int AvailablePoints => GameState.Get("talent points", () => GameState.H.TalentsHandler.availablePoints) ?? ScreenAvailable;

    public static int SpentPoints => GameState.Get("talent points used", () => GameState.H.TalentsHandler.usedTalentPoints) ??
                                     new GameText(Paths.TalentsLoc.PointsLeftTxt).GetParsedTrailingInt() - ScreenAvailable;

    /// <summary>The counter as the screen shows it, "available/spent", for the check against the game.</summary>
    public static string Screen => $"{ScreenAvailable}/{new GameText(Paths.TalentsLoc.PointsLeftTxt).GetParsedTrailingInt() - ScreenAvailable}";

    private static int ScreenAvailable => new GameText(Paths.TalentsLoc.PointsLeftTxt).GetParsedLeadingInt();

    public static IEnumerator Save => new GameButton(Paths.TalentsLoc.SaveBtn).Click();

    public static IEnumerator OpenNode(int catalogIndex) => new GameButton(NodePath(catalogIndex)).Click();

    /// <summary>The game's data behind a node (level, levelCap, unlocked); null when the node isn't there.</summary>
    public static Il2Cpp.Talents NodeData(int catalogIndex) =>
        new GameElement(NodePath(catalogIndex)).TryGetComponent<Il2Cpp.TalentsInteraction>(out var node) ? node.talent : null;

    private static string NodePath(int catalogIndex) => $"{Paths.TalentsLoc.NodeRoot}/talentInteraction ({catalogIndex})";

    public static IEnumerator ClosePreview => new GameButton(Paths.TalentPreviewLoc.CloseBtn).Click();

    public static GameButton UpgradeButton => new(Paths.TalentPreviewLoc.UpgradeBtn);
}
