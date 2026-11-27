using System;
using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Il2Cpp;

namespace Firebot.GameModel.Features.Town;

/// <summary>
///     The town's buildings. Each one opens with the game's own call (GameMenu, the HUD's
///     AdventureScreen.Open*) and, when that fails, by clicking through the town as before. Open is
///     therefore lazy: the town itself only opens when a building needs the click path.
/// </summary>
public static class Town
{
    private static IEnumerator OpenByClick => new GameButton(Paths.BattleLoc.RightSideUILoc.TownBtn).Click();

    /// <summary>A no-op: every Open* below gets the town open itself when it has to click through it.</summary>
    public static IEnumerator Open
    {
        get { yield break; }
    }

    private static IEnumerator ViaTown(Func<IEnumerator> clicks)
    {
        yield return GameMenu.EnsureOpen(MenuName.TownIrongard, () => OpenByClick);
        yield return clicks();
    }

    private static IEnumerator Building(MenuName menu, Action open, Func<IEnumerator> clicks, int submenu = 0) =>
        GameMenu.OpenOrClick(menu, open, () => ViaTown(clicks), submenu);

    private static IEnumerator Click(string path) => new GameButton(path).Click();

    // The Engineer building opens a choice popup first; War Machines are its "garage" card.
    public static IEnumerator OpenEngineer => Building(MenuName.Engineer, () => GameMenu.Adventure.OpenEngineer(),
        () => OpenViaGarageSelection(Paths.MenusLoc.GarageSelectionLoc.OpenEngineerBtn));

    public static IEnumerator OpenWarMachines => Building(MenuName.WarMachines,
        () => GameMenu.Adventure.OpenrWarMachines(WarMachinesMech.SubmenuNum.Garage, null),
        () => OpenViaGarageSelection(Paths.MenusLoc.GarageSelectionLoc.OpenGarageBtn));

    private static IEnumerator OpenViaGarageSelection(string cardPath)
    {
        yield return new GameButton(Paths.MenusLoc.TownIrongardLoc.EngineerBtn).Click();
        yield return new GameButton(cardPath).Click();
    }

    public static IEnumerator OpenMagicQuarters => Building(MenuName.MagicQuarters,
        () => GameMenu.Adventure.OpenMagicQuarters(MagicQuartersMech.SubmenuNum.Info, MagicQuartersMech.OpenOrigin.Town),
        () => Click(Paths.MenusLoc.TownIrongardLoc.MagicQuartersBtn));

    public static IEnumerator OpenOracle => Building(MenuName.Oracle,
        () => GameMenu.Adventure.OpenOracleMech(OracleMech.SubmenuNum.Oracle, OracleMech.OriginPoint.Town),
        () => Click(Paths.MenusLoc.TownIrongardLoc.OracleBtn));

    // No HUD call of its own that the bot needs to tune (the Store's opener takes an origin and an item).
    public static IEnumerator OpenStore => ViaTown(() => Click(Paths.MenusLoc.TownIrongardLoc.StoreBtn));

    public static IEnumerator OpenAlchemist => Building(MenuName.Alchemist,
        () => GameMenu.Adventure.OpenAlchemist(AlchemistMech.SubmenuNum.Experiments),
        () => Click(Paths.MenusLoc.TownIrongardLoc.AlchemistBtn), (int)AlchemistMech.SubmenuNum.Experiments);

    public static IEnumerator OpenLibrary => Building(MenuName.Library,
        () => GameMenu.Adventure.OpenLibrary(LibraryMech.SubmenuNum.FirestoneResearch),
        () => Click(Paths.MenusLoc.TownIrongardLoc.LibraryBtn));

    public static IEnumerator OpenTempleOfEternals => Building(MenuName.TempleOfEternals,
        () => GameMenu.Adventure.OpenTempleOfEternals(),
        () => Click(Paths.MenusLoc.TownIrongardLoc.TempleOfEternalsBtn));

    // The tavern building also opens a choice popup first: the card game or Scarab's Game.
    public static IEnumerator OpenTavern => Building(MenuName.Tavern,
        () => GameMenu.Adventure.OpenTavern(false, TavernMech.OpenOrigin.Town),
        () => OpenViaTavernSelection(Paths.MenusLoc.TavernSelectionLoc.OpenTavernBtn));

    public static IEnumerator OpenScarabGame => OpenScarabGameAndWait();

    // Since 3.64 screens load from bundles and this one can take seconds: read at once, every button
    // of Pharaoh's Vault was still hidden and the run did nothing in 10 s (Steam-17, 05/11).
    private static IEnumerator OpenScarabGameAndWait()
    {
        yield return Building(MenuName.ScarabGame,
            () => GameMenu.Adventure.OpenScarabGame(ScarabGameMech.OpenOrigin.Town, MenuName.ScarabGame),
            () => OpenViaTavernSelection(Paths.MenusLoc.TavernSelectionLoc.OpenScarabGameBtn));
        yield return Poll.Until(() => new GameElement(Paths.ScarabGameLoc.CloseBtn).IsVisible(), ScreenPolls);
    }

    private const int ScreenPolls = 20;

    private static IEnumerator OpenViaTavernSelection(string cardPath)
    {
        yield return new GameButton(Paths.MenusLoc.TownIrongardLoc.TavernBtn).Click();

        var card = new GameButton(cardPath);
        yield return Poll.Until(() => card.IsClickable(), ScreenPolls);
        yield return card.Click();
    }

    public static IEnumerator OpenExoticMerchant => Building(MenuName.ExoticMerchant,
        () => GameMenu.Adventure.OpenExoticMerchant(ExoticMerchantMech.SubmenuNum.SellItems, ExoticMerchantMech.OpenOrigin.Town),
        () => Click(Paths.MenusLoc.TownIrongardLoc.ExoticMerchantBtn));

    // The battles hub (arena, campaign) has no HUD call: through the town.
    public static IEnumerator OpenBattles => ViaTown(() => Click(Paths.MenusLoc.TownIrongardLoc.BattlesBtn));

    // The HUD's call wants a hero id; the building opens the list the task expects.
    public static IEnumerator OpenHallOfHeroes => ViaTown(() => Click(Paths.MenusLoc.TownIrongardLoc.HallOfHeroesBtn));

    public static IEnumerator OpenPirateShip => Building(MenuName.PirateShip,
        () => GameMenu.Adventure.OpenPirateShip(PirateShipMech.SubmenuNum.PiratesPrize),
        () => Click(Paths.MenusLoc.TownIrongardLoc.PirateShipBtn));

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.TownIrongardLoc.CloseBtn).Click();
}
