using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town;

public static class Town
{
    public static IEnumerator Open => new GameButton(Paths.BattleLoc.RightSideUILoc.TownBtn).Click();

    // The Engineer building opens a choice popup first; War Machines are its "garage" card.
    public static IEnumerator OpenEngineer =>
        OpenViaGarageSelection(Paths.MenusLoc.GarageSelectionLoc.OpenEngineerBtn);

    public static IEnumerator OpenWarMachines =>
        OpenViaGarageSelection(Paths.MenusLoc.GarageSelectionLoc.OpenGarageBtn);

    private static IEnumerator OpenViaGarageSelection(string cardPath)
    {
        yield return new GameButton(Paths.MenusLoc.TownIrongardLoc.EngineerBtn).Click();
        yield return new GameButton(cardPath).Click();
    }

    public static IEnumerator OpenMagicQuarters =>
        new GameButton(Paths.MenusLoc.TownIrongardLoc.MagicQuartersBtn).Click();

    public static IEnumerator OpenOracle => new GameButton(Paths.MenusLoc.TownIrongardLoc.OracleBtn).Click();

    public static IEnumerator OpenAlchemist => new GameButton(Paths.MenusLoc.TownIrongardLoc.AlchemistBtn).Click();

    public static IEnumerator OpenLibrary => new GameButton(Paths.MenusLoc.TownIrongardLoc.LibraryBtn).Click();

    public static IEnumerator OpenTempleOfEternals =>
        new GameButton(Paths.MenusLoc.TownIrongardLoc.TempleOfEternalsBtn).Click();

    // The tavern building also opens a choice popup first: the card game or Scarab's Game.
    public static IEnumerator OpenTavern => OpenViaTavernSelection(Paths.MenusLoc.TavernSelectionLoc.OpenTavernBtn);

    public static IEnumerator OpenScarabGame =>
        OpenViaTavernSelection(Paths.MenusLoc.TavernSelectionLoc.OpenScarabGameBtn);

    private static IEnumerator OpenViaTavernSelection(string cardPath)
    {
        yield return new GameButton(Paths.MenusLoc.TownIrongardLoc.TavernBtn).Click();
        yield return new GameButton(cardPath).Click();
    }

    public static IEnumerator OpenExoticMerchant =>
        new GameButton(Paths.MenusLoc.TownIrongardLoc.ExoticMerchantBtn).Click();

    public static IEnumerator OpenBattles => new GameButton(Paths.MenusLoc.TownIrongardLoc.BattlesBtn).Click();

    public static IEnumerator OpenHallOfHeroes =>
        new GameButton(Paths.MenusLoc.TownIrongardLoc.HallOfHeroesBtn).Click();

    public static IEnumerator OpenPirateShip =>
        new GameButton(Paths.MenusLoc.TownIrongardLoc.PirateShipBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.TownIrongardLoc.CloseBtn).Click();
}
