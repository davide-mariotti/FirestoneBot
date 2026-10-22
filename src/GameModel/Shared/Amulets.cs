using System;
using System.Collections.Generic;
using Firebot.GameModel.Base;
using Firebot.Infrastructure;
using Il2Cpp;

namespace Firebot.GameModel.Shared;

/// <summary>
///     The daily amulet, the owned ones and the gems, read from the game's handlers rather than the
///     localized Store text. Codes are AmuletCode names ("AmuletOfConquest").
/// </summary>
public static class Amulets
{
    /// <summary>False when any of the three can't be read: then nothing is bought.</summary>
    public static bool TryRead(out string today, out HashSet<string> owned, out double gems)
    {
        today = null;
        owned = new HashSet<string>();
        gems = -1;
        try
        {
            var loader = GameInitialize.HandlerLoader;
            if (!loader.AmuletDataHandler.TryGetDailyAmuletCode(out var code)) return false;
            today = code.ToString();
            // A dictionary AmuletCode -> Amulet.
            foreach (var entry in loader.AmuletHandler.amulets)
                if (entry.Value.HaveAmulet()) owned.Add(entry.Key.ToString());
            gems = loader.CurrencyHandler.GetCurrency(CurrencyCode.Gem).Amount;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool Owns(string code)
    {
        try
        {
            foreach (var entry in GameInitialize.HandlerLoader.AmuletHandler.amulets)
                if (entry.Key.ToString() == code) return entry.Value.HaveAmulet();
        }
        catch (Exception) { }

        return false;
    }

    /// <summary>The Store card's amulet and its gem price text, or nulls when the card isn't showing.</summary>
    public static (string Code, string Price) ReadCard()
    {
        try
        {
            if (!new GameElement(Paths.MenusLoc.StoreLoc.AmuletsLoc.DailyCard).TryGetComponent(out AmuletStoreInteraction card))
                return (null, null);
            return (card.amulet?.code.ToString(), card.purchaseCostGemsText?.text);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }
}
