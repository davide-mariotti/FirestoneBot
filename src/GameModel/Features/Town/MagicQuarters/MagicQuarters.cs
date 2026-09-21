using System;
using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.MagicQuarters;

public static class MagicQuarters
{
    public static IEnumerator Close => new GameButton(Paths.MenusLoc.MagicQuartersLoc.CloseBtn).Click();

    public static GameButton TrainBtn => new(Paths.MenusLoc.MagicQuartersLoc.TrainBtn);

    public static DateTime NextRunTime => new GameText(Paths.MenusLoc.MagicQuartersLoc.NextRunTimeTxt).Time;

    public static IEnumerator CloseLockedPopup => new GameButton(Paths.MenusLoc.LockedGuardianLoc.CloseBtn).Click();

    public static GameElement Guardians => new(Paths.MenusLoc.MagicQuartersLoc.GuardiansRoot);

    public static IEnumerator OpenChaosRiftTab => new GameButton(Paths.MenusLoc.MagicQuartersLoc.ChaosRiftTabBtn).Click();

    public static GameButton ChaosRiftUpgradeBtn => new(Paths.MenusLoc.MagicQuartersLoc.ChaosRiftUpgradeBtn);
}
