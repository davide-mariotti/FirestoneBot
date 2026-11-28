using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
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

    /// <summary>Position of the visible tree among the carousel's trees; -1 when none shows.</summary>
    public int CurrentTreeIndex => GetChildren().ToList().FindIndex(tree => tree.IsVisible());

    /// <summary>
    ///     The game's research behind every node of every tree, read from each node's
    ///     MeteoriteResearchInteraction instead of opening its preview (GAMESTATE_PLAN.md). Null when
    ///     they can't be read: the caller then scans the previews as before.
    /// </summary>
    public List<(int Tree, int Index, Il2Cpp.MeteoriteResearch Research)> ReadNodes(int nodeCount) =>
        GameState.GetRef("meteorite research nodes", () =>
        {
            var nodes = new List<(int, int, Il2Cpp.MeteoriteResearch)>();
            var root = Root;
            for (var t = 0; root != null && t < root.childCount; t++)
            {
                var tree = root.GetChild(t);
                for (var i = 0; i < nodeCount && PathLineCount + i < tree.childCount; i++)
                    if (tree.GetChild(PathLineCount + i).GetComponentInChildren<Il2Cpp.MeteoriteResearchInteraction>(true)?.meteoriteResearch is { } research)
                        nodes.Add((t, i, research));
            }

            return nodes.Count > 0 ? nodes : throw new InvalidOperationException("no research data behind the nodes");
        });

    public IEnumerator NextTree =>
        new GameButton(Paths.MenusLoc.LibraryLoc.MeteoriteResearchLoc.NextTreeBtn).Click();

    public IEnumerator PreviousTree =>
        new GameButton(Paths.MenusLoc.LibraryLoc.MeteoriteResearchLoc.PreviousTreeBtn).Click();
}
