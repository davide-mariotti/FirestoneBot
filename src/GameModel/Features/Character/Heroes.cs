using System;
using System.Collections.Generic;
using System.Linq;
using Firebot.Tasks.Character;
using Il2Cpp;
using Il2CppBreakInfinity;

namespace Firebot.GameModel.Features.Character;

/// <summary>Hero attributes and the account's hero multipliers, read from the game's HeroDeck (FORMATION_PLAN.md).</summary>
public static class Heroes
{
    public sealed record Snapshot(List<HeroStats> Heroes, double LeaderRatio, List<string> InBattle, string Leader);

    // Attributes outgrow a double (stage 900+), so everything is compared as log10.
    private static double Log(ObscuredBigDouble value)
    {
        BigDouble big = value;
        return big.AbsLog10(); // -Infinity for 0
    }

    private static double Hit(double logDamage, double attackSpeed, double critChance, double logCritDamage) =>
        logDamage - Math.Log10(attackSpeed) + Math.Log10(1 + critChance * Math.Pow(10, logCritDamage));

    /// <summary>Null when anything can't be read: then nothing is decided.</summary>
    public static Snapshot Read()
    {
        try
        {
            var deck = GameInitialize.HandlerLoader.HeroDeck;
            var heroes = new List<HeroStats>();
            var names = new Dictionary<int, string>();
            foreach (var hero in deck.heroes)
            {
                if (!hero.IsUnlocked()) continue;
                names[hero.heroID] = hero.characterName;
                var offense = Hit(Log(hero.damageBasic), hero.attackSpeedBasic, hero.criticalChanceBasic,
                    Log(hero.criticalDamageBasic));
                var potential = Hit(Log(hero.damageInitial) + Log(hero.GetSpecializationMultiplier()) +
                                    Log(hero.GetAttackStyleMultiplier()) + Log(hero.GetResourceMultiplier()),
                    hero.attackSpeedInitial, hero.criticalChanceInitial, Log(hero.criticalDamageInitial));
                heroes.Add(new HeroStats(hero.characterName, hero.attackStyle.ToString() == "Melee", offense, potential));
            }

            // BattleController isn't a Unity object; a hero's battle logic is, and links back to the manager.
            var battle = UnityEngine.Object.FindObjectOfType<HeroBattleLogic>()?.battleHero?.BattleHeroManager;
            if (battle == null || heroes.Count == 0) return null;

            var leader = names.TryGetValue(battle.GetHeroLeaderId(), out var leaderName) ? leaderName : "";
            var inBattle = new List<string>();
            foreach (var battleHero in battle.battleHeroesInBattle)
                if (battleHero?.hero != null) inBattle.Add(battleHero.hero.characterName);
            if (leader != "" && !inBattle.Contains(leader)) inBattle.Add(leader);

            // The leader's damage is its own times leadership / team bonus (Steam-0, 23/10: the total DPS
            // matched 10^(3.36 - 2.18) on Cirilo).
            var ratio = Log(deck.leadershipMultiplier) - Log(deck.teamBonusMultiplier);
            return heroes.Any(h => double.IsNaN(h.Offense) || double.IsNaN(h.Potential)) ? null
                : new Snapshot(heroes, ratio, inBattle, leader);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
