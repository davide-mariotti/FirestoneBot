using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Library.FirestoneResearch;

public class Node : GameElement
{
    public Node() : base(Paths.MenusLoc.LibraryLoc.NodeLoc.Root) { }

    // FirstOrDefault, not First: no tree being visible is a real state (e.g. right after
    // starting a research, the whole screen briefly - or entirely - closes) rather than a
    // "should never happen" one, and callers below already treat a null tree as "nothing to do
    // here yet" instead of crashing on it.
    private GameElement GetTree() => GetChildren().FirstOrDefault(tree => tree.IsVisible());

    // Used to detect a NextTree/PreviousTree click that didn't actually move (e.g. the game
    // blocked it behind a "complete the previous tree first" popup) - the visible tree's name
    // stays the same when that happens. Empty (never equal to a real tree name) if no tree is
    // visible at all.
    public string CurrentTreeName => GetTree()?.Name ?? string.Empty;

    private static GameElement GetGrow(GameElement gameElement) =>
        new(Paths.MenusLoc.LibraryLoc.NodeLoc.Glow, gameElement);

    private static GameText GetCompletedTxt(GameElement gameElement) =>
        new(Paths.MenusLoc.LibraryLoc.NodeLoc.CompletedTxt, gameElement);

    private static GameElement GetProgressBar(GameElement gameElement) =>
        new(Paths.MenusLoc.LibraryLoc.NodeLoc.ProgressBar, gameElement);

    private static bool IsActiveNode(GameElement child)
    {
        if (!child.IsVisible()) return false;

        var grow = GetGrow(child);
        if (grow.IsVisible()) return false;

        var completedTxt = GetCompletedTxt(child);
        if (completedTxt.IsVisible()) return false;

        var progressBar = GetProgressBar(child);
        return progressBar.IsVisible();
    }

    public IEnumerator Select(int index)
    {
        var tree = GetTree();
        if (tree == null) yield break;

        var child = tree.GetChild(index);

        if (!IsActiveNode(child))
            yield break;

        yield return new GameButton(parent: child).Click();
    }

    public IEnumerator NextTree => new GameButton(Paths.MenusLoc.LibraryLoc.NodeLoc.NextTreeBtn).Click();

    public IEnumerator PreviousTree => new GameButton(Paths.MenusLoc.LibraryLoc.NodeLoc.PreviousTreeBtn).Click();
}
