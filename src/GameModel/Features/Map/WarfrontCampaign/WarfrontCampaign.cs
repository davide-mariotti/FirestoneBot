using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Il2Cpp;
using UnityEngine.EventSystems;

namespace Firebot.GameModel.Features.Map.WarfrontCampaign;

/// <summary>
///     The Warfront campaign. Its state comes from the game's own handlers (GameInitialize.HandlerLoader),
///     readable with no screen open; only a fight goes through the UI: pin, preview, fight button.
///     Missions count from 1 here, as in the game's texts; the game's data counts from 0.
/// </summary>
public static class WarfrontCampaign
{
    public static readonly string[] ModeNames = { "Easy", "Normal", "Hard", "Insane", "Nightmare" };

    private static HandlerLoader Handlers => GameInitialize.HandlerLoader;

    private static Il2CppSystem.Collections.Generic.Dictionary<int, WFCampaignMission> Missions =>
        Handlers.WFCampaignMissionHandler.missionsDict;

    public static int Stars => Handlers.WFCampaignStarHandler.totalStars;

    /// <summary>The active squad's battle power, as the squad screen shows it.</summary>
    public static double BattlePower => Handlers.WarMachineHandler.myFormationBattlePower;

    /// <summary>A mission (from 1) and difficulty (Easy 0 .. Nightmare 4), with the game's power requirement.</summary>
    public record Battle(int Mission, int Mode, double Required)
    {
        public override string ToString() => $"{Mission} {ModeNames[Mode]}";
    }

    /// <summary>
    ///     The next difficulty not won on each mission, when it's unlocked: Easy needs the previous
    ///     mission's Easy, any other difficulty only the one before it on the same mission (live 03/10).
    /// </summary>
    public static List<Battle> Unlocked()
    {
        var battles = new List<Battle>();
        for (var i = 0; Missions.ContainsKey(i); i++)
        {
            var mission = Missions[i];
            var mode = Enumerable.Range(0, ModeNames.Length).FirstOrDefault(d => !IsWon(mission, d), -1);
            if (mode < 0) continue;
            if (mode == 0 && i > 0 && !IsWon(Missions[i - 1], 0)) break; // every later mission is locked too
            battles.Add(new Battle(i + 1, mode, mission.modePowerReqDict[(GameMode)mode]));
        }

        return battles;
    }

    public static double Required(int mission, int mode) => Missions[mission - 1].modePowerReqDict[(GameMode)mode];

    /// <summary>
    ///     A squad's power against the enemy's: the requirement is 30% of the enemy's power on Easy 1-10,
    ///     50% on Easy 11-30 and 80% everywhere else (wiki formula, equal to the game's on all 450 pairs),
    ///     so power / requirement alone doesn't compare across those groups.
    /// </summary>
    public static double VsEnemy(double power, int mission, int mode) =>
        power * (mode == 0 && mission <= 10 ? 0.3 : mode == 0 && mission <= 30 ? 0.5 : 0.8) / Required(mission, mode);

    private static bool IsWon(WFCampaignMission mission, int mode) =>
        mission.modesWon.ContainsKey((GameMode)mode) && mission.modesWon[(GameMode)mode];

    public static bool IsPreviewVisible => new GameElement(Paths.WFCampaignMissionPreviewLoc.CloseBtn).IsVisible();

    /// <summary>Opens a mission's preview from its pin, hidden or not; the map's Warfront tab must be open.</summary>
    public static IEnumerator OpenPreview(int mission)
    {
        WFCampaignMissionMapInteraction pin = null;
        yield return Poll.Until(() => (pin = Pin(mission)) != null);
        if (pin == null) yield break;

        pin.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
        yield return Poll.Until(() => IsPreviewVisible);
    }

    private static WFCampaignMissionMapInteraction Pin(int mission) =>
        GameElement.FindTransform(Paths.WFCampaignLoc.PinsRoot)?
            .GetComponentsInChildren<WFCampaignMissionMapInteraction>(true)
            .FirstOrDefault(p => p.mission != null && p.mission.missionIndex == mission - 1);

    /// <summary>The open preview's fight button for a difficulty, only when the game allows that fight now.</summary>
    public static GameButton FightButton(int mode)
    {
        var modes = GameElement.FindTransform(Paths.WFCampaignMissionPreviewLoc.ModesRoot);
        var button = modes == null
            ? null
            : modes.GetComponentsInChildren<WFCampaignModeInteraction>().FirstOrDefault(m => (int)m.mode == mode)?.fightButton;
        return button == null ? null : new GameButton(transform: button.transform);
    }

    /// <summary>A war machine. Power is without the crew: see Strongest.</summary>
    public record Machine(string Code, string Name, Specialization Role, double Power)
    {
        public override string ToString() => $"{Name} {Role} {Power:0}";
    }

    public static List<Machine> Machines()
    {
        var list = Handlers.WarMachineHandler.warMachineData.warMachineList;
        var machines = new List<Machine>();
        for (var i = 0; i < list.Count; i++)
            machines.Add(new Machine(list[i].code, list[i].name, list[i].specialization, list[i].powerNoCrew));
        return machines;
    }

    /// <summary>The active squad, the front spot (0) first.</summary>
    public static List<Machine> Squad()
    {
        var machines = Machines();
        return Spots().OrderBy(s => s.spotIndex)
            .Select(s => machines.FirstOrDefault(m => m.Code == s.code)).Where(m => m != null).ToList();
    }

    /// <summary>Same machines, same one in front and same one last; the order in between doesn't matter.</summary>
    public static bool SameSquad(List<Machine> a, List<Machine> b) =>
        a.Count == b.Count && (a.Count == 0 || (a[0] == b[0] && a[^1] == b[^1] && !a.Except(b).Any()));

    /// <summary>
    ///     The strongest squad (CAMPAIGN_PLAN.md section 8): the strongest tank in front, the strongest
    ///     healer last, the strongest of the rest between them; without a tank or a healer, that spot
    ///     goes to the next strongest. By power without crew, or a new machine, crewless, would never get in.
    /// </summary>
    public static List<Machine> Strongest(List<Machine> machines)
    {
        var byPower = machines.OrderByDescending(m => m.Power).ToList();
        var tank = byPower.FirstOrDefault(m => m.Role == Specialization.Tank);
        var healer = byPower.FirstOrDefault(m => m.Role == Specialization.Healer);
        var middle = byPower.Where(m => m != tank && m != healer).Take(5 - (tank == null ? 0 : 1) - (healer == null ? 0 : 1));
        return new[] { tank }.Concat(middle).Append(healer).Where(m => m != null).ToList();
    }

    /// <summary>Owned heroes, those in a crew of the active squad, and the crew slots of one machine.</summary>
    public static (int Heroes, int Crewed, int PerMachine) Crews()
    {
        var crewed = new HashSet<int>();
        foreach (var spot in Spots())
            for (var i = 0; i < spot.warMachineHeroCodes.Count; i++)
                crewed.Add(spot.warMachineHeroCodes[i]);

        // heroesDict holds every hero in the game (42); one not owned has id 0 (Steam-0 03/10: 10 owned,
        // the 10 in its crews). heroesUnlockState isn't ownership either: 38 true.
        var heroes = 0;
        foreach (var hero in Handlers.HeroServerHandler.heroesDict)
            if (hero.Value != null && hero.Value.id != 0) heroes++;

        return (heroes, crewed.Count, Handlers.EngineerLevel.heroSpots);
    }

    public static bool IsSquadScreenVisible => new GameElement(Paths.SelectWarMachinesLoc.CloseBtn).IsVisible();

    public static bool IsCrewPopupVisible => new GameElement(Paths.SelectWarMachineHeroesLoc.CloseBtn).IsVisible();

    /// <summary>The squad screen, through a mission preview's "Battle formation"; the Warfront tab must be open.</summary>
    public static IEnumerator OpenSquadScreen()
    {
        yield return OpenPreview(1);
        yield return new GameButton(Paths.WFCampaignMissionPreviewLoc.ChangeFormationBtn).Click();
        yield return Poll.Until(() => IsSquadScreenVisible);
    }

    /// <summary>The open squad screen's spots, front (spotIndex 0) first, as the unsaved draft shows them.</summary>
    public static List<WarMachineFormationSettingSpot> ScreenSpots() =>
        GameElement.FindTransform(Paths.SelectWarMachinesLoc.SpotsRoot)?
            .GetComponentsInChildren<WarMachineFormationSettingSpot>().OrderBy(s => s.spotIndex).ToList()
        ?? new List<WarMachineFormationSettingSpot>();

    public static int CrewCount(WarMachineFormationSettingSpot spot) => spot.tempCrewHeroIds?.Count ?? 0;

    /// <summary>The deck card that adds a machine to the squad or takes it out.</summary>
    public static GameButton DeckCard(string code)
    {
        var card = GameElement.FindTransform(Paths.SelectWarMachinesLoc.DeckRoot)?
            .GetComponentsInChildren<WarMachineSelectInteraction>()
            .FirstOrDefault(c => c.warMachine != null && c.warMachine.code == code);
        return card?.clickButton == null ? null : new GameButton(transform: card.clickButton.transform);
    }

    /// <summary>The open crew popup's free heroes: listed but not selected for this crew.</summary>
    public static List<GameButton> FreeHeroCards() =>
        GameElement.FindTransform(Paths.SelectWarMachineHeroesLoc.HeroGridRoot)?
            .GetComponentsInChildren<HeroInteractionSelect>()
            .Where(h => h.hero != null && !h.isEmpty && !h.IsSelected() && h.button != null)
            .Select(h => new GameButton(transform: h.button.transform)).ToList()
        ?? new List<GameButton>();

    private static List<WarMachineSpotData> Spots()
    {
        var list = Handlers.WarMachineHandler.warMachineData.formationList;
        var spots = new List<WarMachineSpotData>();
        for (var i = 0; i < list.Count; i++) spots.Add(list[i]);
        return spots;
    }
}
