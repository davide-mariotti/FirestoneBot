using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Character;

public static class Party
{
    // See UiVariantButton - this bottom-bar HUD variant switches dynamically within a session
    // (confirmed live, 2026-09-17, via the sibling InventoryBtn in the same menuButtons row), not
    // just once per session like the notification rail. Tries every known location instead of
    // assuming one is "the" active one.
    public static IEnumerator Open => UiVariantButton.Click(
        new GameButton(Paths.BattleLoc.BottomRightSideUINewLoc.PartyBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIMobileLoc.PartyBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIDesktopLoc.PartyBtn));

    public static IEnumerator Close => new GameButton(Paths.PartyLoc.CloseBtn).Click();

    private static GameElement Roster => new(Paths.PartyLoc.HeroRosterRoot);

    /// <summary>Indices of heroes currently in the active 5-slot formation, read from each roster
    /// card's "activeIcon" badge - see Paths.PartyLoc.HeroRosterRoot for the live-verification flag
    /// on whether this index lines up 1:1 with Hall of Heroes' own roster order.</summary>
    public static HashSet<int> ActivePartyIndices()
    {
        var result = new HashSet<int>();
        var index = 0;

        foreach (var card in Roster.GetChildren())
        {
            if (new GameElement(path: "bg/activeIcon", parent: card).IsVisible()) result.Add(index);
            index++;
        }

        return result;
    }
}
