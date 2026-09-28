using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Library.MeteoriteResearch;

/// <summary>The Meteorite Research tree carousel: one tree visible at a time, nodes 0..12 in each.</summary>
public class MeteoriteNode : GameElement
{
    // Each tree's children are 13 decorative connectors (researchPath0..12) followed by the 13 real
    // node buttons (research0..12).
    private const int PathLineCount = 13;

    public MeteoriteNode() : base(Paths.MenusLoc.LibraryLoc.MeteoriteResearchLoc.TreesRoot) { }

    // No visible tree is a real state (a "complete tree N first" toast can cover it).
    private GameElement GetTree() => GetChildren().FirstOrDefault(tree => tree.IsVisible());

    // Unchanged after NextTree means the move was refused (the next tree is locked).
    public string CurrentTreeName => GetTree()?.Name ?? string.Empty;

    public IEnumerator Select(int index)
    {
        var tree = GetTree();
        if (tree == null) yield break;

        var child = tree.GetChild(PathLineCount + index);

        if (!child.IsVisible())
            yield break;

        yield return new GameButton(parent: child).Click();
    }

    public IEnumerator NextTree =>
        new GameButton(Paths.MenusLoc.LibraryLoc.MeteoriteResearchLoc.NextTreeBtn).Click();

    public IEnumerator PreviousTree =>
        new GameButton(Paths.MenusLoc.LibraryLoc.MeteoriteResearchLoc.PreviousTreeBtn).Click();
}
