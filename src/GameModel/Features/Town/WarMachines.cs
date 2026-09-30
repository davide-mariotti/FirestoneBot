using System.Collections;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town;

public static class WarMachines
{
    public static IEnumerator Close => new GameButton(Paths.WarMachinesLoc.CloseBtn).Click();

    private static GameElement MachineGrid => new(Paths.WarMachinesLoc.MachineGridRoot);

    public static GameElement[] Machines =>
        MachineGrid.GetChildren().Where(m => m.Name.StartsWith("warMachineSquare (")).ToArray();

    public static IEnumerator SelectMachine(GameElement machine) => new GameButton(parent: machine).Click();

    public static IEnumerator OpenWorkshopTab => new GameButton(Paths.WarMachinesLoc.WorkshopTabBtn).Click();

    public static GameButton LevelUpBtn => new(Paths.WarMachinesLoc.LevelUpBtn);

    public static IEnumerator OpenRarityTab => new GameButton(Paths.WarMachinesLoc.RarityTabBtn).Click();

    public static GameButton RarityBtn => new(Paths.WarMachinesLoc.RarityBtn);

    public static string RarityCost => new GameText(Paths.WarMachinesLoc.RarityCostTxt).GetParsedText();

    public static string RarityCostIcon => IconSprite.NameAt(Paths.WarMachinesLoc.RarityCostIcon);

    // '.'-grouped ("7.476"). Built with the rarity tab: missing before it's opened (Steam-0, 30/09).
    public static double Tools => new GameText(Paths.WarMachinesLoc.ToolsTxt).GetParsedDoubleAbbreviated(-1);
}
