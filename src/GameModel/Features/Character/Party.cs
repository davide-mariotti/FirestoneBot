using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Il2CppTMPro;
using UnityEngine;

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

    /// <summary>
    ///     Makes a deployed hero the leader: a tap on it in its slot shows it in the leader panel, whose
    ///     setLeader button then assigns it - only once the panel shows that hero. Assumed from the
    ///     prefab (23/10), first tried live with the user watching.
    /// </summary>
    public static IEnumerator SetLeader(string name, Action<string> log)
    {
        var slot = SlotOf(name);
        if (slot == null)
        {
            log($"[FAILED] Set leader {name}: not in a formation slot.");
            yield break;
        }

        var mover = slot.Find("battleFormationMover");
        var target = mover != null && mover.gameObject.activeInHierarchy ? mover : slot;
        GameButton.DispatchPointerClick(target.gameObject);
        yield return new WaitForSeconds(1.5f);

        var panel = FindDeep(GameElement.FindTransform(Paths.PartyLoc.Root), "leaderPanel");
        var texts = panel == null ? "" : string.Join(" | ", panel.GetComponentsInChildren<TMP_Text>()
            .Where(t => t.gameObject.activeInHierarchy).Select(t => t.text));
        log($"[INFO] Set leader {name}: tapped {target.name} in {slot.name}; leader panel shows '{texts}'.");

        var setLeader = panel == null ? null : FindDeep(panel, "setLeader");
        if (!texts.Contains(name) || setLeader == null || !setLeader.gameObject.activeInHierarchy)
        {
            log($"[FAILED] Set leader {name}: the panel doesn't show it, or no setLeader button - nothing clicked.");
            yield break;
        }

        yield return new GameButton(transform: setLeader).Click();
        yield return new WaitForSeconds(1.5f);
    }

    private static Transform SlotOf(string name)
    {
        var slots = GameElement.FindTransform(Paths.PartyLoc.HeroSlotsRoot);
        if (slots == null) return null;
        for (var i = 0; i < slots.childCount; i++)
        {
            var hero = slots.GetChild(i).Find(name);
            if (hero != null && hero.gameObject.activeInHierarchy) return slots.GetChild(i);
        }

        return null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (var i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }
}
