using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Character;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using HallOfHeroesModel = Firebot.GameModel.Features.Character.HallOfHeroes;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Character;

/// <summary>
///     Hall of Heroes (https://firestone-idle-rpg.fandom.com/wiki/Hall_of_Heroes) gear/jewel
///     management - the user asked to design this from scratch
///     ("come proponi di muoverci con una logica?"), grounded in the Gear wiki
///     (https://firestone-idle-rpg.fandom.com/wiki/Gear) and the Jewels wiki
///     (https://firestone-idle-rpg.fandom.com/wiki/Jewels), plus the external tips guide already
///     used for Firestone/Meteorite Research.
///     Covers, for every hero in the roster:
///     - Gear tier unlock: T2 (Wrist/Shoulder/Belt) and T3 (Ring/Relic), each gated on both hero
///       power and Meteorites per the wiki - both enforced by the game itself via the unlock
///       button's own clickable state, same as every other gated action in this codebase
///       (PharaohsVault, Awakening, Tree of Life), so no threshold values are hardcoded here.
///     - Gear enchanting: T2/T3 slots always, since their bonus applies to ALL heroes per the Gear
///       wiki's Bonuses table; T1 (Weapon/Chest/Boots) only for heroes currently in the active
///       battle formation (Party screen), since it only benefits the hero wearing it - per the
///       user's explicit choice ("squadra attuale, dinamico") over a fixed hero list, resolving the
///       tension between the tips guide's "concentrate on a few core heroes" and this codebase's
///       usual spread-evenly default. Active-formation membership is read from the separate "Party"
///       screen (GameModel/Features/Character/Party.cs) by index, since neither Party's roster nor
///       Hall of Heroes' own roster exposes a readable hero name to cross-check by - both are
///       ASSUMED to list heroes in the same order (same underlying data, two different screens),
///       not independently verified live. If T1 enchanting turns out to target the wrong heroes,
///       start there.
///     - Jewel enchanting: all 6 slots (Ankh/Rune/Idol T1, Talisman/Necklace/Trinket T2), every
///       hero unconditionally. The Jewels wiki says a jewel's bonus only applies "when the hero is
///       in its crew" (a War Machine), which would argue for the same active-only scoping as gear
///       T1 - but unlike Void Crystals, Ethereal Shards have no other use per the wiki, so there's
///       no opportunity cost being protected by narrowing the target set, and the War Machine crew
///       screen hasn't been investigated. Revisit if this turns out to matter in practice.
///     Same screen, same per-hero navigation, so gear and jewels are handled in one pass per hero
///     instead of two separate tasks re-walking the whole roster.
///     No tier-unlock action exists for jewels (checked twice, both view modes) - only a locked-tier
///     placeholder with no button, unlike gear/soulstones. Soulstones (tier-unlock and enchant, same
///     shape as gear) confirmed present but explicitly deferred by the user to a future task
///     (level 200+).
/// </summary>
public class HallOfHeroesTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    internal override float? MaxRuntimeSeconds => 1800f;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.HallOfHeroes;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    // Safety bound only, matching the pattern used everywhere else in this codebase for a "keep
    // going until nothing's left to do" loop.
    private const int MaxEnchantIterationsPerSlot = 50;

    public override IEnumerator Execute()
    {
        yield return Party.Open;
        var activePartyIndices = Party.ActivePartyIndices();
        yield return Party.Close;

        // Fast path: the notification (when up) opens Hall of Heroes directly. Safe no-op otherwise.
        yield return Notifications.HallOfHeroes;

        // Guaranteed path regardless of the notification - Town -> hallOfHeroes building icon
        // (townBg/parent, 24 icons total). Corrected after the user pointed out live that it's
        // reached through Town - an earlier UnityPy pass missed it by only grepping the building
        // icons already mapped in this codebase instead of dumping the live full list.
        yield return TownScreen.Open;
        yield return TownScreen.OpenHallOfHeroes;

        var heroIndex = 0;

        foreach (var hero in HallOfHeroesModel.Heroes)
        {
            yield return HallOfHeroesModel.SelectHero(hero);

            yield return HallOfHeroesModel.OpenGearTab;
            yield return HallOfHeroesModel.GearTierUnlock.OpenGalleryView;

            if (HallOfHeroesModel.GearTierUnlock.UnlockTier2Btn.IsClickable())
                yield return HallOfHeroesModel.GearTierUnlock.UnlockTier2Btn.Click();

            if (HallOfHeroesModel.GearTierUnlock.UnlockTier3Btn.IsClickable())
                yield return HallOfHeroesModel.GearTierUnlock.UnlockTier3Btn.Click();

            yield return HallOfHeroesModel.OpenEnchantingTab;
            yield return HallOfHeroesModel.GearEnchanting.OpenGearCategory;

            // T2/T3 first for every hero (global bonus, always worth it), T1 only added on top for
            // heroes currently in the active formation. ponytail: this only guarantees the priority
            // order WITHIN a single hero's turn, not across the whole roster - an earlier hero could
            // in theory spend Void Crystals that a later active-party hero's T1 might have wanted.
            // Not worth a full second pass over every hero (doubles the per-hero navigation cost) for
            // a gap that's minor and self-corrects on the next 6h recheck.
            var gearSlots = activePartyIndices.Contains(heroIndex)
                ? HallOfHeroesModel.GearEnchanting.AlwaysEnchantSlots
                    .Concat(HallOfHeroesModel.GearEnchanting.ActivePartyOnlyGearSlots)
                : HallOfHeroesModel.GearEnchanting.AlwaysEnchantSlots;

            foreach (var slot in gearSlots)
                yield return EnchantSlot(HallOfHeroesModel.GearEnchanting.SlotButton(slot));

            yield return HallOfHeroesModel.JewelEnchanting.OpenJewelsCategory;

            foreach (var slot in HallOfHeroesModel.JewelEnchanting.AllSlots)
                yield return EnchantSlot(HallOfHeroesModel.JewelEnchanting.SlotButton(slot));

            heroIndex++;
        }

        yield return HallOfHeroesModel.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private static IEnumerator EnchantSlot(GameButton button)
    {
        var iterations = 0;

        while (button.IsClickable() && iterations < MaxEnchantIterationsPerSlot)
        {
            iterations++;
            yield return button.Click();
        }
    }
}
