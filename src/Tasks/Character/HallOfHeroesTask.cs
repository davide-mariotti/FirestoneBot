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
///     Hall of Heroes, for every hero: unlocks gear tiers 2 and 3 when affordable, enchants gear
///     tiers 2/3 (they boost every hero) and, for heroes in the active formation only, tier 1 (it
///     only boosts its wearer), plus every jewel slot. The formation is read from the Party screen
///     by index, assuming both screens list heroes in the same order - not verified. Soul stones
///     (level 200) aren't handled.
/// </summary>
public class HallOfHeroesTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    // Off by default: never run live, and the gear enchant order still puts the Ring fourth - see
    // HallOfHeroes.GearEnchanting.AlwaysEnchantSlots. Fix that before enabling.
    protected override bool DefaultEnabled => false;

    internal override float? MaxRuntimeSeconds => 1800f;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.HallOfHeroes;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private const int MaxEnchantIterationsPerSlot = 50;

    public override IEnumerator Execute()
    {
        yield return Party.Open;
        var activePartyIndices = Party.ActivePartyIndices();
        yield return Party.Close;

        yield return Notifications.HallOfHeroes;

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

            // ponytail: tiers 2/3 come before tier 1 within each hero, not across the roster - an earlier
            // hero can spend Void Crystals a later hero's tier 1 wanted. Upgrade: a second pass for tier 1
            // after every hero's tiers 2/3, if formation heroes' tier 1 falls behind the others' tiers 2/3.
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
