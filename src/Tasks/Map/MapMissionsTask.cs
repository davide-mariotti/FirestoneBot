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
        yield return Notifications.MapMissions;

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
        if (string.IsNullOrEmpty(value)) return false;

        if (string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)) return false;

        Debug($"[FAILED] Invalid mission_time_order '{value}'. Using default 'desc'.");
        return false;
    }
}
