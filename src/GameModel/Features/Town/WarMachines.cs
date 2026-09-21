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

    /// <summary>Every owned war machine - "nextWarMachineUnlock" and "allWarMachinesButton" (trailing
    /// siblings, not real machines) filtered out by name, confirmed via UnityPy.</summary>
    public static GameElement[] Machines =>
        MachineGrid.GetChildren().Where(m => m.Name.StartsWith("warMachineSquare (")).ToArray();

    public static IEnumerator SelectMachine(GameElement machine) => new GameButton(parent: machine).Click();

    public static IEnumerator OpenWorkshopTab => new GameButton(Paths.WarMachinesLoc.WorkshopTabBtn).Click();

    public static GameButton LevelUpBtn => new(Paths.WarMachinesLoc.LevelUpBtn);
}
