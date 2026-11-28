using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
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

    /// <summary>Position of the visible tree among the carousel's trees; -1 when none shows.</summary>
    public int CurrentTreeIndex => GetChildren().ToList().FindIndex(tree => tree.IsVisible());

    /// <summary>
    ///     The game's research behind every node of every tree, the hidden trees too, read from each
    ///     node's FirestoneResearchInteraction instead of opening its preview (GAMESTATE_PLAN.md).
    ///     Null when they can't be read: the caller then scans the previews as before.
    /// </summary>
    public List<(int Tree, int Index, Il2Cpp.FirestoneResearch Research)> ReadNodes(int nodeCount) =>
        GameState.GetRef("firestone research nodes", () =>
        {
            var nodes = new List<(int, int, Il2Cpp.FirestoneResearch)>();
            var root = Root;
            for (var t = 0; root != null && t < root.childCount; t++)
            {
                var tree = root.GetChild(t);
                for (var i = 1; i <= nodeCount && i < tree.childCount; i++)
                    if (tree.GetChild(i).GetComponentInChildren<Il2Cpp.FirestoneResearchInteraction>(true)?.firestoneResearch is { } research)
                        nodes.Add((t, i, research));
            }

            return nodes.Count > 0 ? nodes : throw new InvalidOperationException("no research data behind the nodes");
        });

    public IEnumerator NextTree => new GameButton(Paths.MenusLoc.LibraryLoc.NodeLoc.NextTreeBtn).Click();

    public IEnumerator PreviousTree => new GameButton(Paths.MenusLoc.LibraryLoc.NodeLoc.PreviousTreeBtn).Click();
}
