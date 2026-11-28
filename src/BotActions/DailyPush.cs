using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Base;
using Firebot.GameModel.Features.Inventory;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Il2Cpp;
using Logger = Firebot.Core.Logger;

namespace Firebot.BotActions;

/// <summary>
///     The daily push at the wall (the user's request, 28/11; GAMESTATE_PLAN.md, "Routine di push con
///     pergamene e consumabili"), the F2P guide's routine: at the farm stage AutoRetreat picked, the
///     three scrolls, Drums of War (it raises the instant gold items) and the other four battle items,
///     then one of each instant gold item from the smallest; the heroes' upgrades spend that gold and
///     AutoRetreat climbs to the top and fights the boss while it all lasts (scrolls 10 min, items 5).
///     Never Midas' Touch (scarce: the user, 28/11). Each use goes through the slot's own button and
///     counts only once the game's amount drops.
/// </summary>
public static class DailyPush
{
    private const string Root = Paths.MenusLoc.Root + "/menus/Inventory";

    private static readonly ScrollCode[] Scrolls = { ScrollCode.Speed, ScrollCode.Damage, ScrollCode.Health };

    private static readonly InventoryItemCode[] Buffs =
    {
        InventoryItemCode.DrumsOfWar, InventoryItemCode.DragonArmor, InventoryItemCode.GuardiansRune,
        InventoryItemCode.TotemOfAgony, InventoryItemCode.TotemOfAnnihilation
    };

    private static readonly InventoryItemCode[] Gold =
    {
        InventoryItemCode.PouchOfGold, InventoryItemCode.BucketOfGold, InventoryItemCode.CrateOfGold, InventoryItemCode.PileOfGold
    };

    private static string PathOf(UnityEngine.Transform t)
    {
        var names = new List<string>();
        for (; t != null && t.name != "menus"; t = t.parent) names.Insert(0, t.name);
        return string.Join("/", names);
    }

    private static int? ScrollAmount(ScrollCode code) => GameState.Get($"scroll {code}", () => GameState.H.ScrollsHandler.scrollItems[code].amount);

    private static bool? ScrollActive(ScrollCode code) => GameState.Get($"scroll {code} active", () => GameState.H.ScrollsHandler.scrollItems[code].IsActive());

    private static int? ItemAmount(InventoryItemCode code) => GameState.Get($"item {code}", () => GameState.H.InventoryItemsHandler.inventoryItems[code].amount);

    private static bool? ItemActive(InventoryItemCode code) => GameState.Get($"item {code} active", () => GameState.H.InventoryItemsHandler.inventoryItems[code].IsActive());

    private static IEnumerator OpenInventory(Action<bool> done)
    {
        yield return Inventory.Open;
        var close = new GameElement(Paths.InventoryLoc.CloseBtn);
        yield return Poll.Until(() => close.IsVisible(), 50, 0.1f);
        done(close.IsVisible());
    }

    private static IEnumerator OpenTab(string tab)
    {
        yield return new GameButton(Root + "/submenuButtons/" + tab).Click();
        yield return Poll.AfterClick("inventory tab");
    }

    /// <summary>
    ///     Clicks one slot of the open tab, named after the code, and waits for the amount to drop; false
    ///     (logged) when it didn't. The slots are pooled cells whose Button has no listener (the scroll's
    ///     onClick did nothing, -27 28/11 04:32): the pointer events, as for the chest slots.
    /// </summary>
    private static IEnumerator Use(string name, Func<int?> amount, Action<bool> done)
    {
        var before = amount();
        if (before is not > 0)
        {
            Logger.Info($"[DailyPush] {name}: none held ({before?.ToString() ?? "unreadable"}) - skipped.");
            done(true);
            yield break;
        }

        // The game holds a short cooldown between two uses (InventoryItemsHandler.globalCooldownOn).
        var slot = new GameButton(Paths.InventoryLoc.ContentRoot + "/" + name);
        yield return Poll.Until(() => slot.IsClickable(), 40, 0.25f);
        if (!slot.IsClickable())
        {
            Logger.Info($"[DailyPush] {name}: the slot isn't clickable - stopped.");
            done(false);
            yield break;
        }

        yield return slot.ClickSimulated();
        yield return Poll.Until(() => amount() < before, 30, 0.2f);
        var after = amount();
        var ok = after < before;
        if (ok) Used++;
        Logger.Info($"[DailyPush] {name}: {before} -> {after}{(ok ? "" : $" - not used, screens: {Firebot.Core.Watchdog.DumpActiveScreens()}")}.");
        done(ok);
    }

    /// <summary>How many uses went through in the last Run.</summary>
    public static int Used { get; private set; }

    /// <summary>"test": one Scroll of Speed and one Drums of War, to see both kinds of slot go through.</summary>
    public static IEnumerator Test()
    {
        var opened = false;
        yield return OpenInventory(ok => opened = ok);
        if (!opened) yield break;
        var ok = false;
        yield return OpenTab("scrolls");
        yield return Use(ScrollCode.Speed.ToString(), () => ScrollAmount(ScrollCode.Speed), r => ok = r);
        if (ok)
        {
            yield return OpenTab("inventoryItems");
            yield return Use(InventoryItemCode.DrumsOfWar.ToString(), () => ItemAmount(InventoryItemCode.DrumsOfWar), _ => { });
        }

        yield return Inventory.Close;
        Logger.Info($"[DailyPush] Test: Speed active {ScrollActive(ScrollCode.Speed)}, Drums of War active {ItemActive(InventoryItemCode.DrumsOfWar)}.");
    }

    /// <summary>The scrolls, buffs and gold items; false when something didn't go through (the rest is left).</summary>
    public static IEnumerator Run(bool gold, Action<bool> done)
    {
        var start = UnityEngine.Time.realtimeSinceStartup;
        Used = 0;
        var opened = false;
        yield return OpenInventory(ok => opened = ok);
        if (!opened)
        {
            Logger.Info("[DailyPush] The inventory didn't open.");
            done(false);
            yield break;
        }

        var ok = true;
        yield return OpenTab("scrolls");
        foreach (var code in Scrolls)
        {
            if (!ok) break;
            if (ScrollActive(code) == true) continue;
            yield return Use(code.ToString(), () => ScrollAmount(code), r => ok = r);
        }

        if (ok) yield return OpenTab("inventoryItems");
        foreach (var code in Buffs)
        {
            if (!ok) break;
            if (ItemActive(code) == true) continue;
            yield return Use(code.ToString(), () => ItemAmount(code), r => ok = r);
        }

        foreach (var code in gold ? Gold : Array.Empty<InventoryItemCode>())
        {
            if (!ok) break;
            yield return Use(code.ToString(), () => ItemAmount(code), r => ok = r);
        }

        yield return Inventory.Close;
        var active = new List<string>();
        foreach (var code in Scrolls) if (ScrollActive(code) == true) active.Add(code.ToString());
        foreach (var code in Buffs) if (ItemActive(code) == true) active.Add(code.ToString());
        Logger.Info($"[DailyPush] Done in {UnityEngine.Time.realtimeSinceStartup - start:0.0} s, all went through: {ok}; active: {string.Join(", ", active)}.");
        done(ok);
    }

    /// <summary>"dry": every tab's slots with the game's own use buttons, nothing used.</summary>
    public static IEnumerator Discover()
    {
        var opened = false;
        yield return OpenInventory(ok => opened = ok);
        if (!opened)
        {
            Logger.Info("[DailyPush] The inventory didn't open.");
            yield break;
        }

        var tabs = new List<string>();
        var bar = GameElement.FindTransform(Root + "/submenuButtons");
        for (var i = 0; bar != null && i < bar.childCount; i++) tabs.Add(bar.GetChild(i).name);
        Logger.Info($"[DailyPush] Inventory tabs: {string.Join(", ", tabs)}.");

        foreach (var tab in tabs)
        {
            yield return OpenTab(tab);

            var slots = new List<string>();
            var content = GameElement.FindTransform(Paths.InventoryLoc.ContentRoot);
            for (var i = 0; content != null && i < content.childCount; i++)
            {
                var slot = content.GetChild(i);
                if (!slot.gameObject.activeInHierarchy) continue;
                var what = slot.name;
                if (slot.GetComponentInChildren<ScrollInteraction>(true) is { } s)
                    what += $" scroll {s.scroll?.scrollItemType} x{s.scroll?.amount}";
                if (slot.GetComponentInChildren<InventoryItemInteraction>(true) is { } it)
                    what += $" item {it.inventoryItem?.inventoryItemType} x{it.inventoryItem?.amount} use " +
                            (it.itemUseButton == null ? "-" : $"{PathOf(it.itemUseButton.transform)} {it.itemUseButton.interactable}");
                var buttons = new List<string>();
                foreach (var b in slot.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    buttons.Add($"{b.name}{(b.gameObject.activeInHierarchy ? "" : "(off)")} interactable {b.interactable} " +
                                $"listeners {b.onClick.GetPersistentEventCount()}");
                if (buttons.Count > 0) what += $" buttons [{string.Join(" ", buttons)}]";
                var components = new List<string>();
                foreach (var c in slot.GetComponents<UnityEngine.Component>()) components.Add(c.GetIl2CppType().Name);
                what += $" components [{string.Join(" ", components)}]";
                slots.Add(what);
            }

            Logger.Info($"[DailyPush] Tab {tab}: {(slots.Count == 0 ? "empty" : string.Join(" | ", slots))}.");
        }

        yield return Inventory.Close;
    }
}
