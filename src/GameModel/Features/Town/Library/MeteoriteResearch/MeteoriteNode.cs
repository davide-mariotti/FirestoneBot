using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Library.MeteoriteResearch;

public class MeteoriteNode : GameElement
{
    // researchPath0..12 (13 decorative connector lines) precede research0..12 (13 real buttons) as
    // siblings under each tree - confirmed via UnityPy child-order dump, no prior precedent (this
    // feature was never automated before).
    private const int PathLineCount = 13;

    public MeteoriteNode() : base(Paths.MenusLoc.LibraryLoc.MeteoriteResearchLoc.TreesRoot) { }

    // FirstOrDefault, not First: no tree visible is a real state (e.g. a "complete tree N first"
    // validation toast covering the tree browser), not a "should never happen" one - see
    // FirestoneResearch.Node, which hit the same crash live and switched to this same pattern.
    private GameElement GetTree() => GetChildren().FirstOrDefault(tree => tree.IsVisible());

    // Used to detect a NextTree click that didn't actually move (tree still locked) - same
    // reasoning and fix as FirestoneResearch.Node.CurrentTreeName.
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
