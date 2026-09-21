using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Base;
using Firebot.GameModel.Features.Map;
using Firebot.GameModel.Features.Map.Missions;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;

namespace Firebot.Tasks.Map;

public class MapMissionsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Map;

    private MelonPreferences_Entry<string> _timeOrder;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.MapMissions;

    public override IEnumerator Execute()
    {
        // Fast path: the notification (when up) opens the World Map directly. Safe no-op otherwise.
        yield return Notifications.MapMissions;

        // Guaranteed path regardless of the notification - same reasoning as the previous tasks:
        // don't rely on the screen/tab already being open/selected.
        yield return WorldMap.Open;
        yield return WorldMap.OpenMapMissionsTab;

        foreach (var mission in ScanMissions(m => m.IsActive))
        {
            if (mission.IsCompleted)
                yield return mission.Select();
            else
            {
                yield return mission.Select();
                var speedupBtn = MissionPreview.SpeedupBtn;

                if (speedupBtn.IsVisible() && MissionPreview.CanSpeedup)
                    yield return speedupBtn.Click();

                yield return MissionRewardsPopup.Close;
            }

            yield return MissionPreview.Close;
        }

        foreach (var mission in ScanMissions(m => !m.IsActive && !m.IsCompleted, true))
        {
            yield return mission.Select();

            if (MissionPreview.IsNotEnoughSquads)
            {
                yield return MissionPreview.Close;
                break;
            }

            yield return MissionPreview.StartMission;
        }

        DateTime? earliest = null;
        yield return FindEarliestMissionProgress(value => earliest = value);
        NextRunTime = earliest ?? MapMission.NextRunTime;

        yield return WorldMap.Close;
    }

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_timeOrder != null) return;

        _timeOrder = category.CreateEntry(
            "mission_time_order",
            "asc",
            "Mission Time Order",
            "Sort missions by time required. Use 'asc' (shorter first) or 'desc' (longer first)."
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

    private IEnumerator FindEarliestMissionProgress(Action<DateTime?> setEarliest)
    {
        DateTime? earliest = null;

        foreach (var mission in ScanMissions(mission => mission.IsActive))
        {
            yield return mission.Select();

            var progress = MissionPreview.NextRunTime;
            if (!earliest.HasValue || progress < earliest.Value)
                earliest = progress.AddSeconds(-BotSettings.FreeSpeedupSeconds);

            yield return MissionPreview.Close;
        }

        setEarliest(earliest);
    }

    private bool IsAscending()
    {
        var value = _timeOrder?.Value?.Trim();
        if (string.IsNullOrEmpty(value)) return true;

        if (string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)) return false;

        Debug($"[FAILED] Invalid mission_time_order '{value}'. Using default 'asc'.");
        return true;
    }
}
