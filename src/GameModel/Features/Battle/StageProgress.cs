using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Battle;

public static class StageProgress
{
    /// <summary>-1 when the stage bar isn't on screen (e.g. bot is in Town) rather than a real stage.</summary>
    // The battle's own stage (MainBattle.CurrentStageNumber: equal to the bar on every probe line since
    // 26/11), the bar's text as the fallback.
    public static int Current => GameState.Get("current stage", () => (int)GameState.H.MainBattle.CurrentStageNumber) is > 0 and var stage
        ? stage
        : new GameText(Paths.BattleLoc.StageProgressionLoc.CurrentStageNumTxt).GetParsedInt(-1);

    public static IEnumerator GoBack => new GameButton(Paths.BattleLoc.StageProgressionLoc.GoBackBtn).Click();

    public static GameButton GoForthBtn => new(Paths.BattleLoc.StageProgressionLoc.GoForthBtn);

    public static GameButton FightBossBtn => new(Paths.BattleLoc.RightSideUILoc.FightBossBtn);
}
