using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

namespace Firebot.GameModel.Shared;

public static class CharacterScreen
{
    /// <summary>
    ///     The avatar click is sometimes ignored right after the scene loads, so this retries. It
    ///     checks IsOpen before every click and never clicks an open screen, which would close it.
    /// </summary>
    public static IEnumerator Open()
    {
        for (var attempt = 0; attempt < 5 && !IsOpen; attempt++)
        {
            yield return new GameButton(Paths.BattleLoc.PlayerAvatarLoc.OpenBtn).Click();
            if (!IsOpen) yield return new WaitForSeconds(2f);
        }
    }

    public static bool IsOpen => new GameElement(Paths.MenusLoc.CharacterLoc.Root).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.CharacterLoc.CloseBtn).Click();

    public static IEnumerator OpenQuestsTab => new GameButton(Paths.MenusLoc.CharacterLoc.QuestsTabBtn).Click();

    public static IEnumerator OpenTalentsTab => new GameButton(Paths.MenusLoc.CharacterLoc.TalentsTabBtn).Click();

    public static IEnumerator OpenDailyQuestsSubTab =>
        new GameButton(Paths.MenusLoc.CharacterLoc.QuestsLoc.DailyTabBtn).Click();

    public static IEnumerator OpenWeeklyQuestsSubTab =>
        new GameButton(Paths.MenusLoc.CharacterLoc.QuestsLoc.WeeklyTabBtn).Click();

    /// <summary>Countdown of whichever quest tab is selected - read it right after selecting one.</summary>
    public static DateTime QuestsRenewTime => new GameText(Paths.MenusLoc.CharacterLoc.QuestsLoc.RenewTxt).Time;

    public static List<GameButton> DailyQuestClaimButtons() =>
        QuestClaimButtons(Paths.MenusLoc.CharacterLoc.QuestsLoc.DailyQuestsGridRoot);

    public static List<GameButton> WeeklyQuestClaimButtons() =>
        QuestClaimButtons(Paths.MenusLoc.CharacterLoc.QuestsLoc.WeeklyQuestsGridRoot);

    // Every quest's claim button; clicking one that isn't complete yet is a no-op.
    private static List<GameButton> QuestClaimButtons(string gridRootPath)
    {
        var grid = new GameElement(gridRootPath);
        var buttons = new List<GameButton>();
        foreach (var quest in grid.GetChildren())
            buttons.Add(new GameButton("claimButton", quest));
        return buttons;
    }
}
