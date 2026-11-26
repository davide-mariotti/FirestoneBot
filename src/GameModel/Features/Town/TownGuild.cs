using System;
using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Il2Cpp;

namespace Firebot.GameModel.Features.Town;

/// <summary>
///     The guild's buildings, opened like the town's (see Town): the game's own call first, the clicks
///     through the guild screen when that fails. Open is lazy for the same reason.
/// </summary>
public static class TownGuild
{
    private static IEnumerator OpenByClick => new GameButton(Paths.BattleLoc.RightSideUILoc.GuildBtn).Click();

    // The guild screen itself, for the buildings with no HUD call of their own (Tree of Life, Bank).
    private static IEnumerator OpenGuild => GameMenu.OpenOrClick(MenuName.TownGuild,
        () => GameMenu.Adventure.OpenGuild(), () => OpenByClick);

    /// <summary>A no-op: every Open* below gets the guild open itself when it has to click through it.</summary>
    public static IEnumerator Open
    {
        get { yield break; }
    }

    private static IEnumerator ViaGuild(string buildingPath)
    {
        yield return GameMenu.EnsureOpen(MenuName.TownGuild, () => OpenGuild);
        yield return new GameButton(buildingPath).Click();
    }

    private static IEnumerator Building(MenuName menu, Action open, string buildingPath) =>
        GameMenu.OpenOrClick(menu, open, () => ViaGuild(buildingPath));

    public static IEnumerator OpenExpeditions => Building(MenuName.Expeditions,
        () => GameMenu.Adventure.OpenGuildExpeditions(), Paths.MenusLoc.TownGuildLoc.ExpeditionsBtn);

    public static IEnumerator OpenArcaneCrystal => Building(MenuName.ArcaneCrystal,
        () => GameMenu.Adventure.OpenArcaneCrystal(), Paths.MenusLoc.TownGuildLoc.ArcaneCrystalBtn);

    public static IEnumerator OpenTreeOfLife => ViaGuild(Paths.MenusLoc.TownGuildLoc.TreeOfLifeBtn);

    public static IEnumerator OpenAwakening => Building(MenuName.Awakening,
        () => GameMenu.Adventure.OpenAwakening(), Paths.MenusLoc.TownGuildLoc.AwakeningBtn);

    public static IEnumerator OpenChaosRift => Building(MenuName.ChaosRift,
        () => GameMenu.Adventure.OpenChaosRift(), Paths.MenusLoc.TownGuildLoc.ChaosRiftBtn);

    public static IEnumerator OpenBank => ViaGuild(Paths.MenusLoc.TownGuildLoc.BankBtn);

    public static IEnumerator OpenForbiddenKnowledge => Building(MenuName.ForbiddenKnowledge,
        () => GameMenu.Adventure.OpenForbiddenKnowledge(), Paths.MenusLoc.TownGuildLoc.ForbiddenKnowledgeBtn);

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.TownGuildLoc.CloseBtn).Click();
}
