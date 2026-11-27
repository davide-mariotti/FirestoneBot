using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Base;
using Firebot.GameModel.Features.Map;
using Firebot.GameModel.Features.Map.Missions;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;

namespace Firebot.Tasks.Map;

/// <summary>
///     Collects finished map missions (free speed-up when one is almost done) and starts new ones,
///     longest first by default, until the squads run out.
/// </summary>
public class MapMissionsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Map;

    private MelonPreferences_Entry<string> _timeOrder;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.MapMissions;

    public override IEnumerator Execute()
    {
        // Nothing the free squads can start (none free, or the map has no mission they fit: missionsOnMap
        // matched the screen's count on every run, 27/11) and no mission done or inside its free window:
        // the map holds nothing to do. Steam-27 27/11 15:02 and 15:40: 6 squads free, the map empty until
        // its 17:13 refresh, opened anyway.
        var ends = GameState.GetRef("map missions in progress", () =>
        {
            var list = new List<DateTime>();
            foreach (var m in GameState.H.MissionsHandler.missionsInProgress)
                list.Add(GameState.Ends(m.timeStarted, m.realTimeReq) ?? DateTime.MinValue);
            return list;
        });
        var freeSquads = MapMission.FreeSquads;
        var startable = GameState.Get("map missions startable", () =>
        {
            var n = 0;
            foreach (var m in GameState.H.MissionsHandler.missionsOnMap)
                if (m.squadsReq <= freeSquads) n++;
            return n;
        });
        if (!IsNotificationVisible() && freeSquads >= 0 && (freeSquads == 0 || startable == 0) && ends != null &&
            ends.All(e => e != DateTime.MinValue && SpeedUpButton.FreeFrom(e) > DateTime.Now))
        {
            var next = EarliestEndFromGame();
            if (freeSquads > 0 && NewMissionsFromGame() is { } newIn && (next == null || newIn < next)) next = newIn;
            NextRunTime = next ?? DateTime.Now.AddMinutes(30);
            Debug($"[INFO] From the game: {freeSquads} free squads, {startable} missions they can start, {ends.Count} running, " +
                  $"next at {NextRunTime:HH:mm:ss} - not opening.");
            yield break;
        }

        yield return Notifications.MapMissions;

        yield return WorldMap.Open;
        yield return WorldMap.OpenMapMissionsTab;

        foreach (var mission in ScanMissions(m => m.IsActive))
        {
            if (mission.IsCompleted)
                yield return mission.Select();
            else
            {
                // The game says how long it still runs: only a mission inside the free speed-up window is
                // worth opening (26/11 Steam-27: every active mission was opened twice a run, 1 s each).
                if (EndsAt(mission) is { } end && SpeedUpButton.FreeFrom(end) > DateTime.Now) continue;

                yield return mission.Select();
                var speedupBtn = MissionPreview.SpeedupBtn;

                if (speedupBtn.IsVisible() && MissionPreview.CanSpeedup && SpeedUpButton.IsFree(speedupBtn.FullPath))
                {
                    Debug($"[INFO] Mission speed-up shows {SpeedUpButton.Describe(speedupBtn.FullPath)}.");
                    yield return speedupBtn.Click();
                }

                yield return MissionRewardsPopup.Close;
            }

            yield return MissionPreview.Close;
        }

        // Missions take 1 or more squads by category: one that needs more than are free is skipped, and a
        // shorter one further down may still fit (Steam-0 03/10: 1 squad of 5 idle behind a 2-squad one).
        var toStart = ScanMissions(m => !m.IsActive && !m.IsCompleted, true).ToList();
        var order = string.Join(", ", toStart.Select(m => $"{m.Name} {m.TimeRequired - DateTime.Now:hh\\:mm} x{m.SquadsRequired}"));
        var free = MapMission.FreeSquads;
        var freeBefore = free;
        var started = 0;
        foreach (var mission in toStart)
        {
            if (free == 0) break;
            var squads = mission.SquadsRequired;
            if (free > 0 && squads > free) continue;

            yield return mission.Select();

            if (MissionPreview.IsNotEnoughSquads)
            {
                yield return MissionPreview.Close;
                continue;
            }

            yield return MissionPreview.StartMission;
            started++;
            if (free > 0) free -= squads;
        }

        if (toStart.Count > 0)
            Debug($"[INFO] Map missions: started {started} of {toStart.Count}, free squads {freeBefore} -> {MapMission.FreeSquads}, " +
                  $"in order {order}.");

        DateTime? earliest = EarliestEndFromGame();
        if (earliest == null) yield return FindEarliestMissionProgress(value => earliest = value);
        // With none running the screen's countdown is the map's refresh; squads left free wait for it too.
        var refresh = NewMissionsFromGame();
        if (earliest == null) GameState.CompareTimes("map missions refresh", refresh, MapMission.NextRunTime);
        if (MapMission.FreeSquads > 0 && refresh is { } r && (earliest == null || r < earliest)) earliest = r;
        NextRunTime = earliest ?? MapMission.NextRunTime;

        yield return WorldMap.Close;
    }

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_timeOrder != null) return;

        _timeOrder = category.CreateEntry(
            "mission_time_order",
            "desc",
            "Mission Time Order",
            "Order new missions by time required: 'desc' (longest first, the default) or 'asc'. " +
            "Longer missions give better chests and far more honor (1-2 for a short adventure, up to " +
            "32), so filling the squads with short ones leaves the valuable missions unstarted."
        );
    }

    private IEnumerable<MissionPin> ScanMissions(Func<MissionPin, bool> filter = null, bool sortByTime = false)
    {
        var missionRoot = new GameElement(Paths.MissionPinLoc.Root);
        var results = missionRoot.GetChildren().Where(root => root.IsVisible())
            .SelectMany(parent => parent.GetChildren().Where(child => child.IsVisible()))
            .Select(pin => new MissionPin(parent: pin))
            .Where(mission => filter == null || filter(mission))
            .ToList();

        if (sortByTime)
            results = IsAscending()
                ? results.OrderBy(m => m.TimeRequired).ToList()
                : results.OrderByDescending(m => m.TimeRequired).ToList();

        foreach (var mission in results) yield return mission;
    }

    /// <summary>When the game says a map mission in progress ends, local time; null when it can't.</summary>
    private static DateTime? EndsAt(MissionPin pin) => GameState.Get("map mission end", () =>
    {
        if (!pin.TryGetComponent(out Il2Cpp.MapMissionInteraction interaction)) throw new InvalidOperationException("no pin interaction");
        foreach (var m in GameState.H.MissionsHandler.missionsInProgress)
            if (m.missionType == interaction.missionType)
                return GameState.Ends(m.timeStarted, m.realTimeReq) ?? throw new InvalidOperationException("no server time");
        throw new InvalidOperationException($"{interaction.missionType} not in progress");
    });

    /// <summary>
    ///     The free speed-up window of the first mission in progress to end, from the game's data
    ///     (checked 26/11 against the previews: 00:40 both); null when it can't say, and the previews
    ///     are read instead.
    /// </summary>
    private static DateTime? EarliestEndFromGame()
    {
        var ends = GameState.GetRef("map missions end", () =>
        {
            var list = new List<DateTime>();
            foreach (var m in GameState.H.MissionsHandler.missionsInProgress)
                if (GameState.Ends(m.timeStarted, m.realTimeReq) is { } end)
                    list.Add(end);
            return list;
        });
        if (ends is not { Count: > 0 }) return null;

        // A later free window that opens before the first mission ends catches both in one visit: two
        // missions 2 minutes apart cost two 7 s visits (Steam-27 27/11: 12:33 and 12:35, 12:45 and 12:47).
        // The first one then waits at most the window, done or still free to speed up.
        var firstEnd = ends.Min();
        return ends.Select(SpeedUpButton.FreeFrom).Where(w => w <= firstEnd.AddSeconds(-20)).DefaultIfEmpty(SpeedUpButton.FreeFrom(firstEnd)).Max();
    }

    /// <summary>When the map gets its new missions, from the game; null when unreadable.</summary>
    private static DateTime? NewMissionsFromGame() => GameState.Get("map missions refresh",
        () => DateTime.Now.AddSeconds(GameState.H.MissionsHandler.TimeLeftForNewMissions));

    private IEnumerator FindEarliestMissionProgress(Action<DateTime?> setEarliest)
    {
        DateTime? earliest = null;

        foreach (var mission in ScanMissions(mission => mission.IsActive))
        {
            yield return mission.Select();

            var progress = SpeedUpButton.FreeFrom(MissionPreview.NextRunTime);
            if (progress != DateTime.MinValue && (!earliest.HasValue || progress < earliest.Value))
                earliest = progress;

            yield return MissionPreview.Close;
        }

        setEarliest(earliest);
    }

    private bool IsAscending()
    {
        var value = _timeOrder?.Value?.Trim();
        if (string.IsNullOrEmpty(value)) return false;

        if (string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)) return false;

        Debug($"[FAILED] Invalid mission_time_order '{value}'. Using default 'desc'.");
        return false;
    }
}
