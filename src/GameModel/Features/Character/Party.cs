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

    // A formation slot's own children; any other one is the deployed hero's spine (see PartyLoc.HeroSlotsRoot).
    private static readonly HashSet<string> SlotParts = new()
    {
        "base", "characterSpinePos", "arrowParent", "isLeader", "battleFormationMover", "slotNumber",
        "characterShadow", "SkeletonContainer", "attributeGlobal"
    };

    /// <summary>The formation's hero names, as far as the screen has filled them in.</summary>
    public static HashSet<string> FormationNames()
    {
        var names = new HashSet<string>();
        var slots = GameElement.FindTransform(Paths.PartyLoc.HeroSlotsRoot);
        if (slots == null) return names;

        for (var i = 0; i < slots.childCount; i++)
        {
            var slot = slots.GetChild(i);
            for (var j = 0; j < slot.childCount; j++)
            {
                var child = slot.GetChild(j);
                if (child.gameObject.activeInHierarchy && !SlotParts.Contains(child.name)) names.Add(child.name);
            }
        }

        return names;
    }

    public const int FormationSize = 5;
}
