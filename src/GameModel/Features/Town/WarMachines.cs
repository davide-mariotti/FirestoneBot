using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Il2Cpp;

namespace Firebot.GameModel.Features.Town;

public static class WarMachines
{
    public static IEnumerator Close => new GameButton(Paths.WarMachinesLoc.CloseBtn).Click();

    private static GameElement MachineGrid => new(Paths.WarMachinesLoc.MachineGridRoot);

    public static GameElement[] Machines =>
        MachineGrid.GetChildren().Where(m => m.Name.StartsWith("warMachineSquare (")).ToArray();

    public static IEnumerator SelectMachine(GameElement machine) => new GameButton(parent: machine).Click();

    public static WarMachineInteractionSquare[] Squares =>
        GameElement.FindTransform(Paths.WarMachinesLoc.MachineGridRoot)?.GetComponentsInChildren<WarMachineInteractionSquare>(true)
        ?? new WarMachineInteractionSquare[0];

    /// <summary>Selects a machine by its code, through its grid square.</summary>
    public static IEnumerator SelectMachine(string code)
    {
        var square = Squares.FirstOrDefault(s => s.warMachine != null && s.warMachine.code == code);
        if (square?.button == null) yield break;
        yield return new GameButton(transform: square.button.transform).Click();
    }

    /// <summary>The game's own handler behind a square's click, for a square the click didn't select.</summary>
    public static void InvokeSquare(string code) =>
        Squares.FirstOrDefault(s => s.warMachine != null && s.warMachine.code == code)?.InvokeAction();

    /// <summary>Which machine the blueprint tab shows, for the logs.</summary>
    public static string BlueprintTabMachine =>
        GameElement.FindTransform(Paths.WarMachinesLoc.BlueprintSubmenu)?
            .GetComponentsInChildren<WMBlueprintUpgradeInteraction>()
            .Select(r => r.upgrade?.machineRef?.name).FirstOrDefault(n => n != null) ?? "none";

    public static IEnumerator OpenWorkshopTab => new GameButton(Paths.WarMachinesLoc.WorkshopTabBtn).Click();

    public static GameButton LevelUpBtn => new(Paths.WarMachinesLoc.LevelUpBtn);

    public static IEnumerator OpenRarityTab => new GameButton(Paths.WarMachinesLoc.RarityTabBtn).Click();

    public static GameButton RarityBtn => new(Paths.WarMachinesLoc.RarityBtn);

    public static string RarityCost => new GameText(Paths.WarMachinesLoc.RarityCostTxt).GetParsedText();

    public static string RarityCostIcon => IconSprite.NameAt(Paths.WarMachinesLoc.RarityCostIcon);

    // '.'-grouped ("7.476"). Built with the rarity tab: missing before it's opened (Steam-0, 30/09).
    public static double Tools => new GameText(Paths.WarMachinesLoc.ToolsTxt).GetParsedDoubleAbbreviated(-1);

    public static IEnumerator OpenBlueprintTab => new GameButton(Paths.WarMachinesLoc.BlueprintTabBtn).Click();

    public static double Blueprints => GameInitialize.HandlerLoader.CurrencyHandler.GetCurrency(CurrencyCode.Blueprint).Amount;

    /// <summary>
    ///     Every owned machine's blueprint upgrades below their cap, from the game's data. The cap
    ///     starts at 5 and rises by 5 every 5 machine levels (wiki).
    /// </summary>
    public static List<WMBlueprintUpgrade> OpenBlueprintUpgrades()
    {
        var list = GameInitialize.HandlerLoader.WarMachineHandler.warMachineData.warMachineList;
        var upgrades = new List<WMBlueprintUpgrade>();
        for (var i = 0; i < list.Count; i++)
            foreach (var upgrade in list[i].blueprintUpgradesDict)
                if (!upgrade.Value.ReachedSoftCap()) upgrades.Add(upgrade.Value);
        return upgrades;
    }

    /// <summary>The open blueprint tab's row for one attribute, once the tab shows that machine.</summary>
    public static WMBlueprintUpgradeInteraction BlueprintRow(string code, int attribute) =>
        GameElement.FindTransform(Paths.WarMachinesLoc.BlueprintSubmenu)?
            .GetComponentsInChildren<WMBlueprintUpgradeInteraction>()
            .FirstOrDefault(r => r.upgrade != null && r.upgrade.machineRef != null &&
                                 r.upgrade.machineRef.code == code && (int)r.upgrade.code == attribute);
}
