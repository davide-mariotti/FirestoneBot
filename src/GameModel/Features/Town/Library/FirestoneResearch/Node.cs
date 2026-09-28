using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Library.FirestoneResearch;

/// <summary>The Firestone Research tree carousel: one tree visible at a time, nodes 1..16 in each.</summary>
public class Node : GameElement
{
    public Node() : base(Paths.MenusLoc.LibraryLoc.NodeLoc.Root) { }

    // No visible tree is a real state (right after starting a research the screen can close), so
    // callers get null rather than an exception.
    private GameElement GetTree() => GetChildren().FirstOrDefault(tree => tree.IsVisible());

    // Unchanged after NextTree/PreviousTree means the move was refused (the next tree is locked).
    public string CurrentTreeName => GetTree()?.Name ?? string.Empty;

    private static GameElement GetGlow(GameElement gameElement) =>
        new(Paths.MenusLoc.LibraryLoc.NodeLoc.Glow, gameElement);

    private static GameText GetCompletedTxt(GameElement gameElement) =>
        new(Paths.MenusLoc.LibraryLoc.NodeLoc.CompletedTxt, gameElement);

    private static GameElement GetProgressBar(GameElement gameElement) =>
        new(Paths.MenusLoc.LibraryLoc.NodeLoc.ProgressBar, gameElement);

    // A pickable node: visible, with a progress bar, and neither glowing nor showing its completed text.
    private static bool IsActiveNode(GameElement child)
    {
        if (!child.IsVisible()) return false;

        var glow = GetGlow(child);
        if (glow.IsVisible()) return false;

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
