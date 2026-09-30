using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Character;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;
using UnityEngine;
using static Firebot.Core.BotSettings;
using HallOfHeroesModel = Firebot.GameModel.Features.Character.HallOfHeroes;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Character;

/// <summary>
///     Hall of Heroes (HALL_OF_HEROES_PLAN.md): Void Crystals on gear enchants and tier unlocks,
///     Ethereal Shards on jewel enchants. EnchantPlanner decides up front exactly what the balances
///     buy, from a snapshot of every hero kept in the config and fully re-read at most every 24 h or
///     when the roster grows. Every click is checked against the screen first: a stale snapshot costs
///     a wasted visit, never a wrong spend. Soul stones (level 200) aren't handled.
/// </summary>
public class HallOfHeroesTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Character;

    // Off until the live test of the plan's section 9 has passed.
    protected override bool DefaultEnabled => false;

    internal override float? MaxRuntimeSeconds => 1800f;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.HallOfHeroes;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private static readonly TimeSpan FullScanInterval = TimeSpan.FromHours(24);

    // Re-plans after the screen disagreed with the snapshot, per run.
    private const int MaxReplans = 5;

    private MelonPreferences_Entry<string> _heroSnapshot;
    private MelonPreferences_Entry<string> _heroSnapshotTime;

    // One run's state: the heroes in grid order, and why spending stopped.
    private List<HeroState> _heroes;
    private int _replansLeft;
    private bool _replan;
    private bool _stop;
    private bool _rosterMoved;
    private bool _currencyOut;
    private bool _gearBlocked;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_heroSnapshot != null) return;

        _heroSnapshot = category.CreateEntry(
            "hero_snapshot",
            "",
            "Hero Snapshot",
            "(auto-managed, don't edit) - every hero's enchant levels, locked tiers, gear power and formation"
        );

        _heroSnapshotTime = category.CreateEntry(
            "hero_snapshot_time",
            "",
            "Hero Snapshot Time",
            "(auto-managed, don't edit) - when hero_snapshot was last fully rebuilt"
        );
    }

    public override IEnumerator Execute()
    {
        _replansLeft = MaxReplans;
        _stop = _rosterMoved = false;

        yield return Party.Open;
        // ponytail: a formation of fewer than 5 waits the whole 8 s every run; read "Deployed: N/5" once
        // it stops counting up if that ever matters.
        yield return Poll.Until(() => Party.FormationNames().Count >= Party.FormationSize, 16);
        var formation = Party.FormationNames();
        yield return Party.Close;

        _heroes = EnchantPlanner.Parse(_heroSnapshot.Value);
        if (formation.Count == 0)
        {
            // Seen right after a start, while the start-up popups still covered the Party button.
            formation = _heroes?.Where(h => h.InFormation).Select(h => h.Name).ToHashSet() ?? new HashSet<string>();
            Debug($"[FAILED] The formation didn't read - keeping the snapshot's: {string.Join(", ", formation)}.");
        }
        else
            Debug($"[INFO] Formation: {string.Join(", ", formation)}.");

        yield return Notifications.HallOfHeroes;
        yield return TownScreen.Open;
        yield return TownScreen.OpenHallOfHeroes;
        yield return Poll.Until(() => HallOfHeroesModel.HeroCount > 0);

        var heroCount = HallOfHeroesModel.HeroCount;
        if (!HallOfHeroesModel.IsOpen || heroCount == 0)
        {
            Debug("[FAILED] Hall of Heroes didn't open (or shows no hero) - retrying later.");
            yield break;
        }

        var fresh = DateTime.TryParse(_heroSnapshotTime.Value, out var readAt) && DateTime.Now - readAt < FullScanInterval;

        var fullRead = _heroes == null || _heroes.Count != heroCount || !fresh;
        if (fullRead)
        {
            Debug($"[INFO] Full read of {heroCount} heroes: " + (_heroes == null ? "no snapshot."
                : _heroes.Count != heroCount ? $"the snapshot has {_heroes.Count}." : "snapshot older than 24 h."));
            yield return FullRead(heroCount, formation);
        }
        else
            _heroes = _heroes.Select(h => h with { InFormation = formation.Contains(h.Name) }).ToList();

        if (!_stop) yield return UnlockTiers(!fullRead);
        if (!_stop && !_gearBlocked) yield return Spend(EnchantCategory.Gear);
        if (!_stop) yield return Spend(EnchantCategory.Jewels);

        _heroSnapshot.Value = EnchantPlanner.Format(_heroes);

        yield return HallOfHeroesModel.Close;
        yield return TownScreen.Close;

        // A roster that no longer matches the snapshot gets its full read on the short retry.
        NextRunTime = _rosterMoved ? DateTime.MinValue : DateTime.Now + RecheckDelay;
    }

    // Saves the snapshot after every hero, but stamps it only once complete: a read cut short by the
    // timeout leaves a snapshot with too few heroes, which the next run reads again from scratch.
    private IEnumerator FullRead(int heroCount, HashSet<string> formation)
    {
        _heroes = new List<HeroState>();
        var table = new List<string>();

        for (var i = 0; i < heroCount; i++)
        {
            if (!HallOfHeroesModel.TrySelectHero(i))
            {
                Debug($"[FAILED] Hero cell {i} of {heroCount} can't be clicked - full read abandoned.");
                _stop = true;
                yield break;
            }

            yield return new WaitForSeconds(InteractionDelay);

            var name = EnchantPlanner.SafeName(HallOfHeroesModel.HeroName);
            if (name.Length == 0)
            {
                Debug($"[FAILED] Hero cell {i}: no name on screen - full read abandoned.");
                _stop = true;
                yield break;
            }

            var hero = new HeroState(name, formation.Contains(name), 0,
                new int[HeroState.GearSlots], new int[HeroState.JewelSlots]);
            _heroes.Add(hero);

            yield return ReadHero(hero, table);
            _heroSnapshot.Value = EnchantPlanner.Format(_heroes);
        }

        _heroSnapshotTime.Value = DateTime.Now.ToString("O");
        Debug($"[INFO] Hall of Heroes, {_heroes.Count} heroes ('*' = formation; level('badge'/'cost'), " +
              "M = empty or needs a higher rarity, L = tier locked):\n" + string.Join("\n", table));
    }

    /// <summary>Everything about the selected hero: gear power and locked tiers (unlocking one it has reached), then every enchant row.</summary>
    private IEnumerator ReadHero(HeroState hero, List<string> table)
    {
        yield return HallOfHeroesModel.Gallery.Open();
        yield return ReadPowerAndUnlock(hero);

        var gearTier2Locked = HallOfHeroesModel.Gallery.IsGearTierLocked(2);
        var gearTier3Locked = HallOfHeroesModel.Gallery.IsGearTierLocked(3);
        var jewelTier2Locked = HallOfHeroesModel.Gallery.IsJewelTier2Locked;
        var line = $"  {hero.Name}{(hero.InFormation ? "*" : "")} power {hero.GearPower} " +
                   $"('{HallOfHeroesModel.Gallery.GearPowerTxt.GetParsedText()}')";

        foreach (var category in new[] { EnchantCategory.Gear, EnchantCategory.Jewels })
        {
            yield return HallOfHeroesModel.Enchanting.Open(category == EnchantCategory.Jewels);

            var levels = hero.Levels(category);
            var shown = new List<string>();
            for (var slot = 0; slot < levels.Length; slot++)
            {
                var tier = EnchantPlanner.Tier(category, slot);
                var locked = category == EnchantCategory.Jewels
                    ? tier == 2 && jewelTier2Locked
                    : (tier == 2 && gearTier2Locked) || (tier == 3 && gearTier3Locked);

                if (locked)
                {
                    levels[slot] = HeroState.Locked;
                    shown.Add($"{EnchantPlanner.SlotName(category, slot)} L");
                }
                else
                {
                    levels[slot] = ReadSlot(hero, category, slot, out var raw);
                    shown.Add(raw);
                }
            }

            line += $" | {string.Join(", ", shown)}";
        }

        table?.Add(line);
    }

    /// <summary>
    ///     The row's level as the screen shows it, or Unavailable: an empty slot, an item that needs a
    ///     higher rarity, an unreadable level, or a cost that disagrees with the table. raw is the
    ///     game's own texts, for the log.
    /// </summary>
    // ponytail: a cost off the wiki table is logged and the slot skipped until the next full read, not
    // bought at the screen's price; give HeroState per-slot costs if a discount ever shows up.
    private int ReadSlot(HeroState hero, EnchantCategory category, int slot, out string raw)
    {
        var jewels = category == EnchantCategory.Jewels;
        var name = EnchantPlanner.SlotName(category, slot);

        if (!HallOfHeroesModel.Enchanting.IsRowShown(jewels, name))
        {
            raw = $"{name} M(empty)";
            return HeroState.Unavailable;
        }

        var level = HallOfHeroesModel.Enchanting.Level(jewels, name, out var levelText);
        var costText = HallOfHeroesModel.Enchanting.CostTxt(jewels, name).GetParsedText();
        raw = $"{name} {level}('{levelText}'/'{costText}')";

        if (HallOfHeroesModel.Enchanting.NeedsHigherRarity(jewels, name))
        {
            raw = $"{name} M(rarity, {level})";
            return HeroState.Unavailable;
        }

        if (level < 0)
        {
            Debug($"[FAILED] {hero.Name} {name}: level badge '{levelText}' isn't a number.");
            return HeroState.Unavailable;
        }

        if (level >= EnchantPlanner.MaxLevel) return level;

        var cost = (long)StringUtils.ParseAbbreviated(costText, -1);
        var expected = EnchantPlanner.Cost(category, slot, level);
        if (cost == expected) return level;

        Debug($"[FAILED] {hero.Name} {name} level {level}: the game asks '{costText}', the table {expected} - " +
              "skipped until the next full read.");
        return HeroState.Unavailable;
    }

    /// <summary>Reads the selected hero's gear power on the open gallery, and unlocks its next tier once it's reached.</summary>
    private IEnumerator ReadPowerAndUnlock(HeroState hero)
    {
        var text = HallOfHeroesModel.Gallery.GearPowerTxt.GetParsedText();
        var power = StringUtils.ParseAbbreviated(text, -1);
        if (power < 0) Debug($"[FAILED] {hero.Name}: gear power '{text}' isn't a number.");
        hero.GearPower = (int)Math.Max(power, 0);

        var tier = HallOfHeroesModel.Gallery.IsGearTierLocked(2) ? 2
            : HallOfHeroesModel.Gallery.IsGearTierLocked(3) ? 3
            : 0;
        if (tier == 0) yield break;

        var threshold = tier == 2 ? EnchantPlanner.Tier2PowerRequired : EnchantPlanner.Tier3PowerRequired;
        Debug($"[INFO] {hero.Name}: gear tier {tier} locked, gear power {hero.GearPower} ('{text}') of {threshold}. " +
              $"Panel: '{HallOfHeroesModel.Gallery.GearTierLockedText(tier)}'.");
        if (hero.GearPower < threshold) yield break;

        yield return HallOfHeroesModel.Gallery.UnlockGearTierBtn(tier).Click();
        yield return Poll.Until(() => HallOfHeroesModel.TierUnlockPopup.IsOpen);

        var popup = HallOfHeroesModel.TierUnlockPopup.IsOpen;
        var currency = HallOfHeroesModel.TierUnlockPopup.CurrencyIconName;
        if (popup)
            Debug($"[INFO] {hero.Name}: unlock popup '{HallOfHeroesModel.TierUnlockPopup.PowerRequirement}', cost " +
                  $"'{HallOfHeroesModel.TierUnlockPopup.Cost}' ({currency}).");

        // Paid in Meteorites, which MeteoriteResearchTask never spends below min_meteorite_reserve. The
        // icon check is what keeps this button from ever spending anything else.
        if (popup && currency.Contains("meteor", StringComparison.OrdinalIgnoreCase))
            yield return HallOfHeroesModel.TierUnlockPopup.ConfirmBtn.Click();
        else
            Debug($"[FAILED] {hero.Name}: no unlock popup paid in Meteorites (icon '{currency}') - not confirmed. " +
                  $"Open: {Watchdog.DumpActiveScreens()}");

        if (CurrencyMissingPopup.IsShowing) yield return CurrencyMissingPopup.Close;
        if (HallOfHeroesModel.TierUnlockPopup.IsOpen) yield return HallOfHeroesModel.TierUnlockPopup.Close;

        Debug(HallOfHeroesModel.Gallery.IsGearTierLocked(tier)
            ? $"[INFO] {hero.Name}: gear tier {tier} still locked."
            : $"[INFO] {hero.Name}: gear tier {tier} unlocked.");
    }

    /// <summary>
    ///     Class 0: heroes short of the gear power for their next tier get Void Crystals before
    ///     anything else, closest to it first, one enchant at a time with the power read again after
    ///     each. Running out of crystals here blocks the other gear enchants for this run.
    /// </summary>
    // ponytail: re-reading the power costs 4 clicks per enchant, but only a few times in a hero's life;
    // estimate the gain from the piece's own power (list view) if it ever gets slow.
    private IEnumerator UnlockTiers(bool retryUnlocks)
    {
        _gearBlocked = false;
        var nothingToEnchant = new HashSet<string>();

        // Heroes already at their next tier's gear power: the unlock itself is tried again on every run
        // (a full read has just tried it), since it can have failed for want of Meteorites.
        var ready = retryUnlocks
            ? _heroes.Where(h => EnchantPlanner.NextTierThreshold(h) is { } t && h.GearPower >= t).ToList()
            : new List<HeroState>();
        foreach (var hero in ready)
        {
            yield return SelectHero(hero);
            if (_stop) yield break;
            yield return ReadHeroAfterGain(hero, EnchantPlanner.NextTierThreshold(hero));
        }

        while (!_stop)
        {
            var hero = EnchantPlanner.GatingOrder(_heroes).FirstOrDefault(h => !nothingToEnchant.Contains(h.Name));
            if (hero == null) yield break;

            var step = EnchantPlanner.NextGatingSlot(hero);
            if (step == null)
            {
                Debug($"[INFO] {hero.Name}: gear power {hero.GearPower} short of its next tier, but no slot to enchant.");
                nothingToEnchant.Add(hero.Name);
                continue;
            }

            var balance = ReadBalance(EnchantCategory.Gear);
            if (balance < step.Cost)
            {
                Debug($"[INFO] Tier unlock: {hero.Name} needs {step.Cost} Void Crystals for its " +
                      $"{EnchantPlanner.SlotName(step.Category, step.Slot)}, {balance} left - no other gear enchant this run.");
                _gearBlocked = true;
                yield break;
            }

            _replan = _currencyOut = false;
            yield return RunSteps(new[] { step });
            if (_stop || _currencyOut || (_replan && --_replansLeft < 0))
            {
                _gearBlocked = true;
                yield break;
            }

            if (_replan) continue;

            yield return ReadHeroAfterGain(hero, EnchantPlanner.NextTierThreshold(hero));
        }
    }

    // After a tier enchant: the new power, an unlock if it's reached, and the new tier's rows if it opened.
    private IEnumerator ReadHeroAfterGain(HeroState hero, int? threshold)
    {
        yield return HallOfHeroesModel.Gallery.Open();
        yield return ReadPowerAndUnlock(hero);

        var stillLocked = threshold == EnchantPlanner.Tier2PowerRequired
            ? HallOfHeroesModel.Gallery.IsGearTierLocked(2)
            : HallOfHeroesModel.Gallery.IsGearTierLocked(3);
        if (!stillLocked) yield return ReadHero(hero, null);

        _heroSnapshot.Value = EnchantPlanner.Format(_heroes);
    }

    /// <summary>Class 1: the plan for what's left of this category's balance, re-made when the screen disagrees.</summary>
    private IEnumerator Spend(EnchantCategory category)
    {
        _currencyOut = false;

        while (!_stop && !_currencyOut)
        {
            var balance = ReadBalance(category);
            if (balance < 0) yield break;

            var plan = EnchantPlanner.Plan(_heroes, category, balance);
            Debug($"[INFO] {category} plan: " + (plan.Count == 0 ? "nothing affordable."
                : string.Join(", ", plan.Select(s =>
                    $"{EnchantPlanner.SlotName(s.Category, s.Slot)} {s.Hero.Name} {s.FromLevel}->{s.FromLevel + 1} ({s.Cost})"))));
            if (plan.Count == 0) yield break;

            // Grouping by hero doesn't change what is bought, only saves visits: the plan already fits the balance.
            _replan = false;
            foreach (var steps in plan.GroupBy(s => s.Hero.Name))
            {
                yield return RunSteps(steps.ToList());
                if (_replan || _stop || _currencyOut) break;
            }

            if (!_replan) yield break;
            if (--_replansLeft < 0)
            {
                Debug($"[INFO] Re-planned {MaxReplans} times this run - no more enchants until the next one.");
                _stop = true;
            }
        }
    }

    /// <summary>
    ///     One hero's steps, in plan order. Clicks only when the screen shows the planned level and a
    ///     clickable button; anything else corrects the snapshot and asks for a new plan (_replan).
    /// </summary>
    private IEnumerator RunSteps(IReadOnlyList<EnchantStep> steps)
    {
        var hero = steps[0].Hero;
        yield return SelectHero(hero);
        if (_stop) yield break;

        EnchantCategory? open = null;
        foreach (var step in steps)
        {
            var jewels = step.Category == EnchantCategory.Jewels;
            if (open != step.Category)
            {
                yield return HallOfHeroesModel.Enchanting.Open(jewels);
                open = step.Category;
            }

            var name = EnchantPlanner.SlotName(step.Category, step.Slot);
            var levels = hero.Levels(step.Category);
            var level = ReadSlot(hero, step.Category, step.Slot, out var raw);
            var button = HallOfHeroesModel.Enchanting.EnchantBtn(jewels, name);

            if (level != step.FromLevel || !button.IsClickable())
            {
                Debug($"[INFO] {hero.Name} {name}: planned at level {step.FromLevel} for {step.Cost}, the screen shows " +
                      $"{raw}{(level == step.FromLevel ? " and a button that can't be clicked" : "")} - re-planning.");
                levels[step.Slot] = level;
                _replan = true;
                yield break;
            }

            yield return button.Click();

            if (CurrencyMissingPopup.IsShowing)
            {
                yield return CurrencyMissingPopup.Close;
                Debug($"[FAILED] {hero.Name} {name}: the game asked for more {Currency(step.Category)} - the balance " +
                      $"or the cost table is wrong, no more {step.Category} enchants this run.");
                _currencyOut = true;
                yield break;
            }

            // Judged on the badge alone: the new level can already be the item's rarity cap (M).
            yield return Poll.Until(() => HallOfHeroesModel.Enchanting.Level(jewels, name, out _) != step.FromLevel);
            var levelAfter = HallOfHeroesModel.Enchanting.Level(jewels, name, out _);
            levels[step.Slot] = ReadSlot(hero, step.Category, step.Slot, out var rawAfter);

            if (levelAfter != step.FromLevel + 1)
            {
                Debug($"[FAILED] {hero.Name} {name}: level {step.FromLevel} before the click, now {rawAfter} - re-planning.");
                _replan = true;
                yield break;
            }

            Debug($"[INFO] {name} {hero.Name} {step.FromLevel}->{step.FromLevel + 1} ({step.Cost} {Currency(step.Category)}), now {rawAfter}.");
            _heroSnapshot.Value = EnchantPlanner.Format(_heroes);
        }
    }

    // By grid position, then checked by name: a roster that moved means the snapshot is stale.
    private IEnumerator SelectHero(HeroState hero)
    {
        var index = _heroes.FindIndex(h => h.Name == hero.Name);
        HallOfHeroesModel.TrySelectHero(index);
        yield return new WaitForSeconds(InteractionDelay);

        var shown = EnchantPlanner.SafeName(HallOfHeroesModel.HeroName);
        if (shown == hero.Name) yield break;

        Debug($"[FAILED] Hero cell {index} shows '{shown}', the snapshot expects '{hero.Name}' - full read on the next run.");
        _heroSnapshotTime.Value = "";
        _stop = _rosterMoved = true;
    }

    /// <summary>The counter at the top, -1 (and a [FAILED] line) when it doesn't read as a number.</summary>
    private long ReadBalance(EnchantCategory category)
    {
        var text = (category == EnchantCategory.Gear
            ? HallOfHeroesModel.VoidCrystalsTxt
            : HallOfHeroesModel.EtherealShardsTxt).GetParsedText();
        var balance = (long)StringUtils.ParseAbbreviated(text, -1);

        Debug(balance < 0
            ? $"[FAILED] {Currency(category)} balance '{text}' isn't a number - no {category} enchants this run."
            : $"[INFO] {Currency(category)}: {balance} ('{text}').");
        return balance;
    }

    private static string Currency(EnchantCategory category) =>
        category == EnchantCategory.Gear ? "Void Crystals" : "Ethereal Shards";
}
