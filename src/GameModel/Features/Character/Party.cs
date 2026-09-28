using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Character;

public static class Party
{
    public static IEnumerator Open => UiVariantButton.Click(
        new GameButton(Paths.BattleLoc.BottomRightSideUINewLoc.PartyBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIMobileLoc.PartyBtn),
        new GameButton(Paths.BattleLoc.BottomSideUIDesktopLoc.PartyBtn));

    public static IEnumerator Close => new GameButton(Paths.PartyLoc.CloseBtn).Click();

    private static GameElement Roster => new(Paths.PartyLoc.HeroRosterRoot);

    /// <summary>Roster indices of the heroes in the active formation (see PartyLoc.HeroRosterRoot).</summary>
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
