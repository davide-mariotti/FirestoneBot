using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Il2Cpp;

namespace Firebot.GameModel.Features.Town.MagicQuarters;

public static class MagicQuarters
{
    public static IEnumerator Close => new GameButton(Paths.MenusLoc.MagicQuartersLoc.CloseBtn).Click();

    public static GameButton TrainBtn => new(Paths.MenusLoc.MagicQuartersLoc.TrainBtn);

    public static GameButton EnlightenmentBtn => new(Paths.MenusLoc.MagicQuartersLoc.EnlightenmentBtn);

    public static DateTime NextRunTime => new GameText(Paths.MenusLoc.MagicQuartersLoc.NextRunTimeTxt).Time;

    public static IEnumerator CloseLockedPopup => new GameButton(Paths.MenusLoc.LockedGuardianLoc.CloseBtn).Click();

    public static GameElement Guardians => new(Paths.MenusLoc.MagicQuartersLoc.GuardiansRoot);

    public static IEnumerator OpenChaosRiftTab => new GameButton(Paths.MenusLoc.MagicQuartersLoc.ChaosRiftTabBtn).Click();

    public static GameButton ChaosRiftUpgradeBtn => new(Paths.MenusLoc.MagicQuartersLoc.ChaosRiftUpgradeBtn);

    public static IEnumerator OpenEvolutionTab => new GameButton(Paths.MenusLoc.MagicQuartersLoc.EvolutionTabBtn).Click();

    public static GameButton EvolveBtn => new(Paths.MenusLoc.MagicQuartersLoc.EvolveBtn);

    public static string EvolveCost => new GameText(Paths.MenusLoc.MagicQuartersLoc.EvolveCostTxt).GetParsedText();

    public static string EvolveCostIcon => IconSprite.NameAt(Paths.MenusLoc.MagicQuartersLoc.EvolveCostIcon);

    /// <summary>The unlocked guardians as the game has them (GuardianDeck), Magic Quarters closed; null when unreadable.</summary>
    public static List<Guardian> Unlocked => GameState.GetRef("guardians", () =>
    {
        var list = new List<Guardian>();
        foreach (var code in new[] { GuardianCode.Vermilion, GuardianCode.Grace, GuardianCode.Ankaa, GuardianCode.Azhar })
            if (GameState.H.GuardianDeck.GetGuardian(code) is { } g && g.IsUnlocked)
                list.Add(g);
        return list;
    });

    /// <summary>The cheapest next holy damage level among the unlocked guardians, in Orbs of Light; null when unreadable.</summary>
    public static double? CheapestHolyUpgrade => GameState.Get("holy damage cost", () =>
        Unlocked is { Count: > 0 } guardians ? guardians.Min(g => g.GetHolyDamageUpgradeCost(1)) : double.PositiveInfinity);

    public static double? OrbsOfLight => GameState.Get("orbs of light", () => GameState.H.CurrencyHandler.GetCurrency(CurrencyCode.OrbOfLight).Amount);

    /// <summary>When the training running now ends, from the game's own timer (Magic Quarters' info submenu).</summary>
    public static DateTime? TrainingEnd => GameState.Get("guardian training end", () =>
    {
        var info = GameInitialize.MenuLoader.MagicQuartersInfoSubmenu;
        return (info.lastTrainGuardinaTime, (double)info.TrainCooldownDuration);
    }) is { } t ? GameState.Ends(t.Item1, t.Item2) : null;

    /// <summary>The squares of the open Magic Quarters with their guardian.</summary>
    public static List<GuardianInteractionSquare> Squares() =>
        (GameElement.FindTransform(Paths.MenusLoc.MagicQuartersLoc.GuardiansRoot)?
            .GetComponentsInChildren<GuardianInteractionSquare>(true) ?? new GuardianInteractionSquare[0])
        .Where(s => s.guardian != null).ToList();
}
