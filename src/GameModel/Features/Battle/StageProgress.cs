using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Battle;

public static class StageProgress
{
    /// <summary>-1 when the stage bar isn't on screen (e.g. bot is in Town) rather than a real stage.</summary>
    public static int Current => new GameText(Paths.BattleLoc.StageProgressionLoc.CurrentStageNumTxt).GetParsedInt(-1);

    public static IEnumerator GoBack => new GameButton(Paths.BattleLoc.StageProgressionLoc.GoBackBtn).Click();
}
