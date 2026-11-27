using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Firebot.GameModel.Features.Battle;
using Firebot.GameModel.Features.Character;
using Firebot.GameModel.Shared;
using Il2Cpp;

namespace Firebot.Core;

/// <summary>
///     Phase 0 of GAMESTATE_PLAN.md: logs the game's own fields next to what the bot reads off the
///     screen today, so the plan's open questions get answered from data. Read-only, changes no
///     decision; goes away once the phase is done. Every line starts with "[Probe]".
/// </summary>
internal static class GameStateProbe
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(5);
    private static DateTime _next;
    private static bool _versionLogged;
    private static string _skips;
    private static string _abilities;

    private static HandlerLoader H => GameInitialize.HandlerLoader;
    private static CoreFunctionalityLoader Core => GameInitialize.CoreFunctionality;

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    // Each read in its own try: one renamed field must not hide the others (plan rule 2).
    private static void Line(string topic, Func<string> read)
    {
        string text;
        try
        {
            text = read();
        }
        catch (Exception e)
        {
            text = $"ERR {e.GetType().Name}: {e.Message}";
        }

        Logger.Info($"[Probe] {topic}: {text}");
    }

    // Both are also called from the scheduler loop and the click path, outside Line(): never throw.
    public static string MenuStack()
    {
        try
        {
            var stack = Core?.NavigationControl?.menuStack;
            if (stack == null) return "?";
            var names = new List<string>();
            foreach (var id in stack) names.Add(((MenuName)id).ToString());
            return "[" + string.Join(">", names) + "]";
        }
        catch (Exception e)
        {
            return "ERR " + e.GetType().Name;
        }
    }

    public static string BlockReasons()
    {
        try
        {
            var blocker = Core?.PlayerActivityBlocker;
            if (blocker == null) return "?";
            var reasons = new List<string>();
            foreach (var r in blocker.activeBlockReasons) reasons.Add(r.ToString());
            foreach (var r in blocker.activeNavigationBlockReasons) reasons.Add("nav:" + r);
            return string.Join(",", reasons);
        }
        catch (Exception e)
        {
            return "ERR " + e.GetType().Name;
        }
    }

    public static void Tick()
    {
        if (DateTime.Now < _next) return;
        _next = DateTime.Now + Every;

        if (!_versionLogged)
        {
            _versionLogged = true;
            Line("version", () => $"{BuildSettings.gameVersion} '{BuildSettings.gameVersionText}'");
        }

        Line("time", () =>
        {
            var su = Core.ServerUpdate;
            return $"serverTimeNow={F(su.serverTimeNow)} unixNow={DateTimeOffset.UtcNow.ToUnixTimeSeconds()} " +
                   $"nextGameDay={su.nextGameDay} leftNextDay={F(su.timeLeftForNextDay)} nextGameWeek={su.nextGameWeek}";
        });

        Line("socket", () =>
        {
            var s = Core.SocketAPI;
            return $"connected={s.IsConnectedAndNotReconnecting()} reconnecting={s.isWaitingToReconnect} loaded={s.socketDataLoaded}";
        });

        Line("ui", () => $"menus={MenuStack()} blocker={BlockReasons()} active={Core.PlayerActivityBlocker.IsActive()}");

        Line("notif", () =>
        {
            // HasNotification throws KeyNotFound for a type the game never registered (PartyManagement
            // on 26/11), so only the registered ones are asked.
            var nh = H.NotificationHandler;
            var has = new List<string>();
            var active = new List<string>();
            var registered = 0;
            foreach (var kv in NotificationHandler.notifications)
            {
                registered++;
                var t = kv.Key;
                try
                {
                    if (nh.HasNotification(t)) has.Add(t.ToString());
                    if (NotificationHandler.IsNotificationActive(t)) active.Add(t.ToString());
                }
                catch (Exception e)
                {
                    has.Add($"{t}:ERR {e.GetType().Name}");
                }
            }

            return $"registered={registered} has=[{string.Join(",", has)}] active=[{string.Join(",", active)}] rail=[{Notifications.Lit}] " +
                   $"canClaimTools={nh.canClaimTools} oracleGift={nh.hasOracleGiftNotification} ritual={nh.hasRitualNotification}";
        });

        Line("research", () =>
        {
            var parts = new List<string>();
            var i = 0;
            foreach (var slot in H.FirestoneResearchHandler.firestoneResearchSlots)
            {
                var r = slot.firestoneResearch;
                parts.Add($"#{i++} on={slot.researchOnProgress} left={F(slot.timeLeft)} started={F(slot.timeStarted)} " +
                          $"{(r == null ? "-" : $"{r.firestoneResearchType} lv{r.level}/{r.levelCap} req={F(r.realTimeRequired)}")}");
            }

            return string.Join(" | ", parts);
        });

        Line("experiments", () =>
        {
            var parts = new List<string>();
            foreach (var kv in H.AlchemyExperimentHandler.experimentSlot)
            {
                var s = kv.Value;
                parts.Add($"{kv.Key} state={s.ExperimentState} left={F(s.timeLeft)} req={F(s.timeRequired)} started={F(s.timeStarted)}");
            }

            return string.Join(" | ", parts);
        });

        Line("missions", () =>
        {
            var mh = H.MissionsHandler;
            var parts = new List<string>();
            foreach (var m in mh.missionsInProgress)
                parts.Add($"{m.missionType} {m.missionState} req={F(m.realTimeReq)} passed={F(m.timePassed)} started={F(m.timeStarted)}");
            return $"squads {mh.availableSquads.GetDecrypted()}/{mh.totalSquads.GetDecrypted()} newIn={mh.TimeLeftForNewMissions} " +
                   $"onMap={mh.missionsOnMap.Count} inProgress: {string.Join(" | ", parts)}";
        });

        Line("expedition", () =>
        {
            var d = H.GuildManagement.expeditionData;
            var a = d.activeExpedition;
            var pending = new List<string>();
            foreach (var e in d.expeditions)
                pending.Add($"{e.expeditionType} tok={e.expeditionTokenReward.GetDecrypted()} coin={e.guildCoinReward.GetDecrypted()} t={F(e.timeReq.GetDecrypted())}");
            return $"active={(a == null ? "-" : a.expeditionType.ToString())} start={F(d.activeExpeditionStartTime)} " +
                   $"passed={F(d.activeExpeditionTimePassed)} refreshed={F(d.expeditionsTimeRefreshed)} pending: {string.Join(" | ", pending)}";
        });

        Line("oracle", () =>
        {
            var o = H.OracleHandler;
            var r = o.activeRitual;
            var gift = H.OraclesGiftDailyClaimHandler;
            var box = H.MysteryBoxDailyClaimHandler;
            return $"ritual={(r == null ? "-" : $"{r.oracleRitualCode} {r.state} started={F(r.timeStarted)}")} " +
                   $"duration={OracleHandler.ritualDuration} queued={o.ritualsQueued?.Count} cycleStart={F(o.cycleStart)} " +
                   $"gift last={F(gift.lastClaim)} expect={gift.expectClaim} box last={F(box.lastClaim)} expect={box.expectClaim}";
        });

        Line("engineer", () =>
        {
            var w = H.WarMachineHandler;
            return $"lastClaimedTime={F(w.lastClaimedTime)} timeReq={F(WarMachineHandler.timeReq)} timePassed={F(w.timePassed)}";
        });

        // The found Firestones come from the run's gold: how fast the gold comes at the wall says what farming there is worth.
        Line("empower", () => $"earnedThisRun log10={F(Heroes.Log(H.Firestones.firestonesEarnedThisRun))} " +
                              $"runGold log10={F(Heroes.Log(H.Firestones.firestoneTotalGoldEarnings))} firstFirestoneGold log10={F(Math.Log10(Firestones.goldForTheFirstFirestone))}");

        Line("talents", () =>
        {
            var t = H.TalentsHandler;
            var spendable = 0;
            foreach (var kv in t.talents)
                if (kv.Value.unlocked && !kv.Value.IsMaxLevel())
                    spendable++;
            return $"points={t.availablePoints} used={t.usedTalentPoints} finished={t.FinishedTheTalentTree()} unlockedNotMax={spendable}";
        });

        Line("wfloot", () =>
        {
            var w = H.WFCampaignMissionHandler;
            return $"claimed={w.hasClaimedLoot} last={F(w.lastLootClaimTimestamp)} loaded={w.lootTimeHasLoaded}";
        });

        Line("stage", () =>
        {
            var b = H.MainBattle;
            return $"game={b.CurrentStageNumber}/{b.MaxStageNumber} wave={b.CurrentStageWave} ui={StageProgress.Current} farming={b.FarmingWave}";
        });

        // The climb is ~137 min a run at ~7.5 stages a minute (-27 27/11): how much the skips give now,
        // and what the Exotic Merchant's skip upgrades would add, before choosing what to buy.
        string skipsNow;
        try
        {
            var skips = new List<string>();
            foreach (var tree in H.ExoticUpgradesHandler.exoticTrees)
                foreach (var u in tree.Value)
                    if (u.Key.ToString().StartsWith("Skip"))
                        skips.Add($"{tree.Key}/{u.Key}@{u.Value.stage} lvl {u.Value.level}/{ExoticUpgrade.levelCap} effect {F(Heroes.Log(u.Value.effect))} " +
                                  $"cost {u.Value.upgradeCostSingle} can {u.Value.CanUpgrade()}");
            var research = new List<string>();
            foreach (var tree in H.FirestoneResearchHandler.researchTrees)
                foreach (var r in tree.Value)
                    if (r.Key.ToString().StartsWith("Skip"))
                        research.Add($"{tree.Key}/{r.Key} lvl {r.Value.level}/{r.Value.levelCap} effect {F(Heroes.Log(r.Value.effect))} unlocked {r.Value.IsUnlocked}");
            var meteorite = new List<string>();
            foreach (var tree in H.MeteoriteResearchHandler.researchTrees)
                foreach (var r in tree.Value)
                    if (r.Key.ToString().StartsWith("Skip"))
                        meteorite.Add($"{tree.Key}/{r.Key} lvl {r.Value.level} effect {F(Heroes.Log(r.Value.currentEffect))} cost {F(r.Value.levelUpCost)} unlocked {r.Value.IsUnlocked()}");
            skipsNow = $"starting stage {BattleMainStageManager.startingStage}; exotic (effect log10) {string.Join(", ", skips)}; " +
                       $"firestone research {string.Join(", ", research)}; meteorite research {string.Join(", ", meteorite)}";
        }
        catch (Exception e)
        {
            skipsNow = $"ERR {e.GetType().Name}: {e.Message}";
        }

        if (skipsNow != _skips) Logger.Info($"[Probe] skips: {_skips = skipsNow}");

        // The formation's damage is ~all one hero's (-27 Talia, -28 Muriel as leader, 27/11): the other
        // slots count for their buffs and debuffs. The deployed heroes' abilities as the game has them,
        // to score those (FORMATION_PLAN.md 4.2).
        string abilitiesNow;
        try
        {
            var battle = UnityEngine.Object.FindObjectOfType<HeroBattleLogic>()?.battleHero?.BattleHeroManager;
            var heroes = new List<string>();
            foreach (var bh in battle.battleHeroesInBattle)
            {
                var list = new List<string>();
                foreach (var a in bh.abilities)
                    if (a != null)
                        list.Add($"{a.name} [{a.abilityType}] cost {F(a.cost)} dur {F(a.duration)} cd {F(a.cooldownDuration)} '{a.description}'");
                heroes.Add($"{bh.hero.characterName}: {string.Join(" | ", list)}");
            }

            abilitiesNow = string.Join(" || ", heroes);
        }
        catch (Exception e)
        {
            abilitiesNow = $"ERR {e.GetType().Name}: {e.Message}";
        }

        if (abilitiesNow != _abilities) Logger.Info($"[Probe] abilities: {_abilities = abilitiesNow}");
    }
}
