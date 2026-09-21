using System;
using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Map.Missions;

public static class MissionPreview
{
    public static IEnumerator Close => new GameButton(Paths.PreviewMissionLoc.CloseBtn).Click();

    public static IEnumerator StartMission => new GameButton(Paths.PreviewMissionLoc.StartBtn).Click();

    public static GameButton SpeedupBtn => new(Paths.PreviewMissionLoc.SpeedupBtn);

    public static bool CanSpeedup => !new GameElement(Paths.PreviewMissionLoc.SpeedupFinishDesc).IsVisible();

    public static bool IsNotEnoughSquads => new GameElement(Paths.PreviewMissionLoc.NotEnoughSquadsTxt).IsVisible();

    public static DateTime NextRunTime => new GameText(Paths.PreviewMissionLoc.NextRunTimeTxt).Time;
}
