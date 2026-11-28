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

    // ---- Rarity: paid in Contracts, capped by the guardian's evolution (Uncommon at 5, Rare at 10, ...).

    public static double? Contracts => GameState.Currency(CurrencyCode.Contract);

    /// <summary>
    ///     The game's own test (MagicQuartersRaritySubmenu.CanUpgrade, a pure check, read here so the
    ///     submenu needn't be loaded): the next rarity within Celestial and the evolution's ceiling, and the
    ///     Contracts its ContractRequirements entry asks.
    /// </summary>
    public static bool? CanRaiseRarity(Guardian g) => GameState.Get($"guardian rarity {g?.guardianID}", () =>
    {
        var next = (int)g.guardianRarity + 1;
        return next <= (int)Rarity.Celestial && next <= (int)g.GetMaxRarityThatCanUpgradeTo() &&
               GameState.H.CurrencyHandler.GetCurrency(CurrencyCode.Contract).Amount >=
               GuardiansRarityEffectsController.ContractRequirements[(Rarity)Math.Max(next, 1)];
    });

    /// <summary>What the next rarity costs, for the log; null past the table.</summary>
    public static int? RarityCost(Guardian g) => GameState.Get($"guardian rarity cost {g?.guardianID}", () =>
        GuardiansRarityEffectsController.ContractRequirements.TryGetValue((Rarity)((int)g.guardianRarity + 1), out var c) ? c : -1) is { } cost and >= 0
        ? cost
        : null;

    /// <summary>The rarity tab's button, from Magic Quarters' own list (SubmenuNum order).</summary>
    public static GameButton RarityTab => GameState.GetRef("magic quarters rarity tab", () =>
        GameInitialize.MenuLoader.MagicQuartersMech.submenusButtonList[(int)MagicQuartersMech.SubmenuNum.Rarity].transform) is { } t
        ? new GameButton(transform: t)
        : null;

    private static MagicQuartersRaritySubmenu RaritySubmenu => GameState.GetRef("magic quarters rarity submenu",
        () => GameInitialize.MenuLoader.MagicQuartersRaritySubmenu);

    /// <summary>The rarity submenu's increase button, once the tab has loaded it.</summary>
    public static GameButton RarityBtn => RaritySubmenu?.rarityButton is { } b && b.gameObject.activeInHierarchy
        ? new GameButton(transform: b.transform)
        : null;

    /// <summary>The rarity-up animation runs: the close button stays hidden meanwhile (Steam-19, 28/11).</summary>
    public static bool RarityAnimating => GameState.Get("magic quarters rarity animation",
        () => GameInitialize.MenuLoader.MagicQuartersMech.playingRarityUpAnimation) == true;

    /// <summary>The guardian the rarity submenu shows; null when it can't say.</summary>
    public static int? RarityGuardian => GameState.Get("magic quarters rarity guardian",
        () => RaritySubmenu.guardianOnPreview.guardianID);

    /// <summary>The sprites under the increase button: the price's currency ("contract..." expected).</summary>
    public static List<string> RarityCostIcons => RaritySubmenu?.rarityButton is { } b
        ? b.GetComponentsInChildren<UnityEngine.UI.Image>(true).Where(i => i.sprite != null).Select(i => i.sprite.name).ToList()
        : new List<string>();

    /// <summary>The squares of the open Magic Quarters with their guardian.</summary>
    public static List<GuardianInteractionSquare> Squares() =>
        (GameElement.FindTransform(Paths.MenusLoc.MagicQuartersLoc.GuardiansRoot)?
            .GetComponentsInChildren<GuardianInteractionSquare>(true) ?? new GuardianInteractionSquare[0])
        .Where(s => s.guardian != null).ToList();
}
