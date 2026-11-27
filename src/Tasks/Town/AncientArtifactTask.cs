using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Il2Cpp;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Creates an Ancient Artifact in the Tavern whenever the 5 elements for one are collected (the
///     bell on "Create artifact" is lit, and the AncientArtifact badge with it). Then, at the user's
///     request (27/11), raises the artifacts the game says can rise: rarity with Golden Keys, levels with
///     Twilight Hourglasses, the cheapest first, each counted once the currency drops.
/// </summary>
public class AncientArtifactTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 15;

    // Elements come from Tavern draws, about 10 a day (GamerQuestTask).
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private const int MaxCreations = 10;
    private const int MaxUpgrades = 30;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.AncientArtifact;

    private enum Upgrade { Rarity, Level }

    /// <summary>What the game would let each owned artifact do now, cheapest first; null when unreadable.</summary>
    private static List<(AncientArtifactCode Code, Upgrade Kind, int Cost)> Ready() => GameState.GetRef("ancient artifacts", () =>
    {
        var ready = new List<(AncientArtifactCode, Upgrade, int)>();
        foreach (var kv in GameState.H.AncientArtifactHandler.ancientArtifacts)
        {
            var a = kv.Value;
            if (a == null || a.level <= 0) continue;
            if (a.CanIncreaseRarity() && a.HaveTheKeysToRarityUp()) ready.Add((kv.Key, Upgrade.Rarity, (int)a.goldenKeysCost));
            if (a.CanIncreaseLevel() && a.HaveTheTwilightHourglassToLevelUp()) ready.Add((kv.Key, Upgrade.Level, a.GetLevelUpCost(1)));
        }

        return ready.OrderBy(r => r.Item2).ThenBy(r => r.Item3).ToList();
    });

    public override IEnumerator Execute()
    {
        var ready = Ready();
        if (!IsNotificationVisible() && ready is { Count: 0 })
        {
            Debug($"[INFO] From the game: nothing to create, no artifact can rise ({GameState.Currency(CurrencyCode.GoldenKey):0} Golden Keys, " +
                  $"{GameState.Currency(CurrencyCode.TwilightHourglass):0} Twilight Hourglasses) - not opening.");
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        if (IsNotificationVisible())
        {
            yield return TownScreen.Open;
            yield return TownScreen.OpenTavern;

            // One click creates it, no confirmation: on Steam-9 (18/10) the bell and the badge went off
            // at once, and the game showed its Achievements menu.
            var created = 0;
            while (created < MaxCreations && Tavern.CanCreateArtifact)
            {
                yield return Tavern.CreateArtifactBtn.Click();
                yield return Poll.Until(() => !Tavern.CanCreateArtifact);
                if (Tavern.CanCreateArtifact) break;
                created++;
            }

            Debug($"[INFO] Ancient Artifact: created {created}, bell still lit={Tavern.CanCreateArtifact}.");

            yield return Tavern.Close;
            yield return TownScreen.Close;
        }

        if (Ready() is { Count: > 0 }) yield return Raise();

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    // On the menu stack, or its screen shown: the Tavern's own button may open it without the stack knowing.
    private static bool ArtifactsOpen =>
        GameState.IsMenuOpen(MenuName.AncientArtifacts) == true ||
        Firebot.GameModel.Base.GameElement.FindTransform(Paths.MenusLoc.Root + "/menus/AncientArtifacts") is { } t && t.gameObject.activeInHierarchy;

    private IEnumerator Raise()
    {
        // The game's navigation alone didn't open it (27/11, Steam-27 and -28): it lives in the Tavern, so
        // from there, by the game's call again, else by the Tavern's artifacts button.
        yield return GameMenu.OpenOrClick(MenuName.AncientArtifacts, null, ViaTavern);
        // Screens load from bundles since 3.64: up to 10 s.
        yield return Poll.Until(() => ArtifactsOpen, 100, 0.1f);
        if (!ArtifactsOpen)
        {
            Debug($"[FAILED] Ancient Artifact: the artifacts screen didn't open (menus: {GameState.MenuStack}).");
            yield break;
        }

        var done = 0;
        var again = 0;
        var start = UnityEngine.Time.realtimeSinceStartup;
        for (var step = 0; step < MaxUpgrades && Ready() is { Count: > 0 } ready; step++)
        {
            var (code, kind, cost) = ready[0];
            var currency = kind == Upgrade.Rarity ? CurrencyCode.GoldenKey : CurrencyCode.TwilightHourglass;
            var before = GameState.Currency(currency) ?? -1;

            // A level was ~2.6 s each (select, its button, the popup, confirm, close; -28 27/11, 25 levels).
            // The popup still open on this same artifact and mode after the last one: one more press of
            // its own button. Anything else closes it and goes the whole way.
            var ok = Confirm(code, kind) == true;
            if (ok) again++;
            else
            {
                yield return CloseTop(MenuName.AAModify);
                yield return UpgradeOnce(code, kind, r => ok = r);
            }

            if (!ok) break;

            yield return Poll.Until(() => GameState.Currency(currency) < before, 30, 0.2f);
            var after = GameState.Currency(currency) ?? -1;
            if (after >= before)
            {
                Debug($"[FAILED] Ancient Artifact: {code} {kind} clicked ({cost} {currency}) but it stayed at {before}.");
                break;
            }

            done++;
            Debug($"[INFO] Ancient Artifact: {code} {kind} up, {currency} {before} -> {after}, popup still open {GameState.IsMenuOpen(MenuName.AAModify)}.");
        }

        yield return CloseTop(MenuName.AAModify);
        Debug($"[INFO] Ancient Artifact: {done} upgrade(s), {again} of them in the popup left open, {UnityEngine.Time.realtimeSinceStartup - start:0.0} s.");
        yield return CloseTop(MenuName.AncientArtifacts);
        yield return Tavern.Close;
        yield return TownScreen.Close;
    }

    // Selects the artifact in the game's own list, presses its rarity or level button and, in the
    // popup that opens for that same artifact and mode, its upgrade button (the UI's own listeners).
    private IEnumerator UpgradeOnce(AncientArtifactCode code, Upgrade kind, Action<bool> done)
    {
        var selected = GameState.Get("artifact select", () =>
        {
            foreach (var item in GameInitialize.MenuLoader.AncientArtifactsMech.artifactInteractions)
                if (item != null && item.artifactOnSlot == code)
                {
                    item.button.onClick.Invoke();
                    return true;
                }

            return false;
        });
        if (selected != true)
        {
            Debug($"[INFO] Ancient Artifact: {code} isn't in the screen's list.");
            done(false);
            yield break;
        }

        yield return Poll.AfterClick("artifact select");

        var pressed = GameState.Get("artifact upgrade button", () =>
        {
            var view = Firebot.GameModel.Base.GameElement.FindTransform(Paths.MenusLoc.Root + "/menus/AncientArtifacts").GetComponentInChildren<AncientArtifactsViewController>(true);
            var button = kind == Upgrade.Rarity ? view.rarityUpButton : view.levelUpButton;
            if (!button.interactable || !button.gameObject.activeInHierarchy) return false;
            button.onClick.Invoke();
            return true;
        });
        if (pressed != true)
        {
            Debug($"[INFO] Ancient Artifact: {code} {kind} button isn't available on screen.");
            done(false);
            yield break;
        }

        yield return Poll.Until(() => GameState.IsMenuOpen(MenuName.AAModify) == true, 30, 0.1f);
        var confirmed = Confirm(code, kind);
        if (confirmed != true)
        {
            Debug($"[INFO] Ancient Artifact: {code} {kind} popup not as expected - nothing bought.");
            yield return CloseTop(MenuName.AAModify);
        }

        done(confirmed == true);
    }

    // In the AAModify popup, open for this same artifact and mode, its upgrade button (the UI's own listener).
    private static bool? Confirm(AncientArtifactCode code, Upgrade kind) => GameState.Get("artifact modify", () =>
    {
        if (GameState.IsMenuOpen(MenuName.AAModify) != true) return false;
        var popup = GameInitialize.MenuLoader.AAModifyPopup;
        var mode = kind == Upgrade.Rarity ? AAModifyPopup.UpgradeMode.Rarity : AAModifyPopup.UpgradeMode.Level;
        if (popup.artifactOnPreview == null || popup.artifactOnPreview.code != code || popup.upgradeMode != mode) return false;
        var modify = Firebot.GameModel.Base.GameElement.FindTransform(Paths.MenusLoc.Root + "/popups/AAModify").GetComponentInChildren<AAModifyViewController>(true).modifyArtifactButton;
        if (!modify.interactable || !modify.gameObject.activeInHierarchy) return false;
        modify.onClick.Invoke();
        return true;
    });

    private static IEnumerator CloseTop(MenuName menu)
    {
        if (GameState.IsMenuOpen(menu) != true) yield break;
        GameState.Get($"close {menu}", () =>
        {
            GameState.Core.NavigationControl.CloseLastOnMenuStack();
            return true;
        });
        yield return Poll.Until(() => GameState.IsMenuOpen(menu) != true, 20, 0.1f);
    }

    private IEnumerator ViaTavern()
    {
        // A second OpenMenu call from inside the Tavern closed the Tavern too (27/11: the stack empty
        // after it), so only the Tavern's own button.
        yield return TownScreen.Open;
        yield return TownScreen.OpenTavern;
        yield return Poll.Until(() => GameState.IsMenuOpen(MenuName.Tavern) == true, 50, 0.1f);

        // The Tavern's own button, found by name next to the shop's (helpCanvas/actionButtons).
        var root = Firebot.GameModel.Base.GameElement.FindTransform(Paths.MenusLoc.Root + "/menus/Tavern/helpCanvas/actionButtons");
        var names = new List<string>();
        for (var i = 0; root != null && i < root.childCount; i++) names.Add(root.GetChild(i).name);
        var target = names.FirstOrDefault(n => n.IndexOf("rtifact", StringComparison.OrdinalIgnoreCase) >= 0);
        Debug($"[INFO] Ancient Artifact: Tavern action buttons [{string.Join(", ", names)}], artifacts '{target ?? "-"}', menus {GameState.MenuStack}.");
        if (target == null) yield break;
        var button = new GameButton(Paths.MenusLoc.Root + "/menus/Tavern/helpCanvas/actionButtons/" + target);
        yield return button.Click();
        yield return Poll.Until(() => ArtifactsOpen, 100, 0.1f);
        // Pooled-looking buttons ignore a plain click (the inventory's slots, Inventory.cs).
        if (!ArtifactsOpen) yield return button.ClickSimulated();
        Debug($"[INFO] Ancient Artifact: after the Tavern's button, menus {GameState.MenuStack}, screen shown {ArtifactsOpen}.");
    }
}
