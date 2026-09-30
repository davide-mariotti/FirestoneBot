#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Firebot.Tasks.Character;

public enum EnchantCategory
{
    Gear,
    Jewels
}

/// <summary>
///     One hero's Hall of Heroes state. Slot values are enchant levels, or Locked (tier not unlocked)
///     or Unavailable (can't be enchanted right now). Gear slots: Weapon, Chest, Boots (tier 1),
///     Wrist, Shoulder, Belt (tier 2), Ring, Relic (tier 3). Jewels: Ankh, Rune, Idol (tier 1),
///     Talisman, Necklace, Trinket (tier 2).
/// </summary>
public sealed record HeroState(string Name, bool InFormation, int GearPower, int[] Gear, int[] Jewels)
{
    public const int Locked = -1;
    public const int Unavailable = -2;

    public const int GearSlots = 8;
    public const int JewelSlots = 6;

    public int GearPower { get; set; } = GearPower;

    public int[] Levels(EnchantCategory category) => category == EnchantCategory.Gear ? Gear : Jewels;
}

public sealed record EnchantStep(HeroState Hero, EnchantCategory Category, int Slot, int FromLevel, long Cost);

/// <summary>
///     Decides, before any click, exactly which enchants a balance buys (HALL_OF_HEROES_PLAN.md,
///     section 4). Pure: no game types, so the tests project compiles it in directly.
/// </summary>
public static class EnchantPlanner
{
    // Gear power a hero needs before its tier can be unlocked (wiki; the unlock popup shows the same).
    public const int Tier2PowerRequired = 1300;
    public const int Tier3PowerRequired = 6600;

    // Tier 1 cost from level L to L+1 (wiki, Gear and Jewels pages; matched some 40 costTexts live on
    // 2026-09-29). Tier 2 costs double, tier 3 four times. Past the table a slot can't be enchanted.
    private static readonly long[] Tier1Costs =
        { 30, 60, 120, 240, 480, 960, 1920, 3840, 5760, 8640, 12960, 19440, 38880, 77760, 116640, 174960 };

    public static int MaxLevel => Tier1Costs.Length;

    // Subtracted from a gear slot's level to rank it: the Ring is worth a level more than the Wrist,
    // Shoulder/Belt three less. Tiers 2 and 3 boost every hero, tier 1 only its wearer. The knobs.
    private static readonly int[] GearAdvantage = { -1, -2, -2, 0, -3, -3, 1, -1 };

    private static readonly string[] GearNames = { "Weapon", "Chest", "Boots", "Wrist", "Shoulder", "Belt", "Ring", "Relic" };
    private static readonly string[] JewelNames = { "Ankh", "Rune", "Idol", "Talisman", "Necklace", "Trinket" };

    public static string SlotName(EnchantCategory category, int slot) =>
        (category == EnchantCategory.Gear ? GearNames : JewelNames)[slot];

    public static int GearTier(int slot) => slot < 3 ? 1 : slot < 6 ? 2 : 3;

    public static int JewelTier(int slot) => slot < 3 ? 1 : 2;

    public static int Tier(EnchantCategory category, int slot) =>
        category == EnchantCategory.Gear ? GearTier(slot) : JewelTier(slot);

    /// <summary>Cost of level -> level+1, or -1 past the table (maxed).</summary>
    public static long Cost(int tier, int level) =>
        level < 0 || level >= Tier1Costs.Length ? -1 : Tier1Costs[level] << (tier - 1);

    public static long Cost(EnchantCategory category, int slot, int level) => Cost(Tier(category, slot), level);

    /// <summary>
    ///     The best candidates first, spent while the balance lasts. Stops at the first candidate it
    ///     can't afford instead of falling back to worse, cheaper ones, so crystals pile up for it.
    /// </summary>
    public static List<EnchantStep> Plan(IReadOnlyList<HeroState> heroes, EnchantCategory category, long balance)
    {
        var levels = heroes.Select(h => (int[])h.Levels(category).Clone()).ToArray();
        var steps = new List<EnchantStep>();

        while (true)
        {
            var (best, heroIndex) = Candidates(heroes, category, levels).FirstOrDefault();
            if (best == null || best.Cost > balance) break; // the stop rule

            steps.Add(best);
            balance -= best.Cost;
            levels[heroIndex][best.Slot]++;
        }

        return steps;
    }

    private static IEnumerable<(EnchantStep Step, int HeroIndex)> Candidates(IReadOnlyList<HeroState> heroes,
        EnchantCategory category, int[][] levels)
    {
        var all =
            from heroIndex in Enumerable.Range(0, heroes.Count)
            let hero = heroes[heroIndex]
            let slots = levels[heroIndex]
            from slot in Enumerable.Range(0, slots.Length)
            where slots[slot] >= 0 && slots[slot] < MaxLevel
            where category == EnchantCategory.Jewels || GearTier(slot) > 1 || hero.InFormation
            let advantage = category == EnchantCategory.Gear ? GearAdvantage[slot] : 0
            select (Step: new EnchantStep(hero, category, slot, slots[slot], Cost(category, slot, slots[slot])),
                Effective: slots[slot] - advantage, HeroIndex: heroIndex);

        var ranked = category == EnchantCategory.Gear
            ? all.OrderBy(c => c.Effective).ThenByDescending(c => c.Step.Hero.InFormation).ThenBy(c => c.Step.Cost)
            : all.OrderBy(c => c.Effective).ThenBy(c => c.Step.Cost).ThenByDescending(c => c.Step.Hero.InFormation);

        return ranked.ThenBy(c => c.Step.Hero.Name, StringComparer.Ordinal).ThenBy(c => c.Step.Slot).Select(c => (c.Step, c.HeroIndex));
    }

    /// <summary>The gear power a hero needs for its next locked tier, or null with no tier locked.</summary>
    public static int? NextTierThreshold(HeroState hero) =>
        hero.Gear[3] == HeroState.Locked ? Tier2PowerRequired
        : hero.Gear[6] == HeroState.Locked ? Tier3PowerRequired
        : null;

    /// <summary>Heroes still short of their next tier's gear power, closest to it first.</summary>
    public static List<HeroState> GatingOrder(IEnumerable<HeroState> heroes) =>
        heroes.Where(h => NextTierThreshold(h) is { } t && h.GearPower < t)
            .OrderBy(h => NextTierThreshold(h)!.Value - h.GearPower)
            .ThenBy(h => h.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    ///     The cheapest unlocked gear slot of a hero working towards a tier, tier 1 included whatever
    ///     the formation; the lower tier wins a tie (more power per crystal). Null if nothing's left.
    /// </summary>
    public static EnchantStep? NextGatingSlot(HeroState hero) =>
        Enumerable.Range(0, hero.Gear.Length)
            .Where(slot => hero.Gear[slot] >= 0 && hero.Gear[slot] < MaxLevel)
            .Select(slot => new EnchantStep(hero, EnchantCategory.Gear, slot, hero.Gear[slot],
                Cost(GearTier(slot), hero.Gear[slot])))
            .OrderBy(s => s.Cost).ThenBy(s => GearTier(s.Slot)).ThenBy(s => s.Slot)
            .FirstOrDefault();

    // "Name*@power:g0,..,g7;j0,..,j5", heroes joined by '|'; '*' marks the formation.
    public static string Format(IEnumerable<HeroState> heroes) => string.Join("|", heroes.Select(h =>
        $"{SafeName(h.Name)}{(h.InFormation ? "*" : "")}@{h.GearPower.ToString(CultureInfo.InvariantCulture)}:" +
        $"{string.Join(",", h.Gear.Select(FormatLevel))};{string.Join(",", h.Jewels.Select(FormatLevel))}"));

    public static string SafeName(string name) => new(name.Where(c => "|:;,@*".IndexOf(c) < 0).ToArray());

    private static string FormatLevel(int level) => level switch
    {
        HeroState.Locked => "L",
        HeroState.Unavailable => "M",
        _ => level.ToString(CultureInfo.InvariantCulture)
    };

    /// <summary>Null when the text is empty or anything in it doesn't read back, which forces a full scan.</summary>
    public static List<HeroState>? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var heroes = new List<HeroState>();
        foreach (var entry in text.Split('|'))
        {
            var at = entry.IndexOf('@');
            var colon = entry.IndexOf(':');
            if (at <= 0 || colon < at) return null;

            var name = entry[..at];
            var inFormation = name.EndsWith("*");
            if (inFormation) name = name[..^1];

            if (!int.TryParse(entry[(at + 1)..colon], NumberStyles.Integer, CultureInfo.InvariantCulture, out var power))
                return null;

            var parts = entry[(colon + 1)..].Split(';');
            if (parts.Length != 2) return null;

            var gear = ParseLevels(parts[0], HeroState.GearSlots);
            var jewels = ParseLevels(parts[1], HeroState.JewelSlots);
            if (name.Length == 0 || gear == null || jewels == null) return null;

            heroes.Add(new HeroState(name, inFormation, power, gear, jewels));
        }

        return heroes;
    }

    private static int[]? ParseLevels(string csv, int count)
    {
        var parts = csv.Split(',');
        if (parts.Length != count) return null;

        var levels = new int[count];
        for (var i = 0; i < count; i++)
        {
            if (parts[i] == "L") levels[i] = HeroState.Locked;
            else if (parts[i] == "M") levels[i] = HeroState.Unavailable;
            else if (int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var level)) levels[i] = level;
            else return null;
        }

        return levels;
    }
}
