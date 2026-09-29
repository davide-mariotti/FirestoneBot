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

    // Only the claimable ones: a quest that isn't complete has its button hidden or disabled, and a
    // click on it would still wait out the interaction delay (~18 s per Quests run on 29/09).
    private static List<GameButton> QuestClaimButtons(string gridRootPath)
    {
        var grid = new GameElement(gridRootPath);
        var buttons = new List<GameButton>();
        foreach (var quest in grid.GetChildren())
        {
            var claimButton = new GameButton(Paths.MenusLoc.CharacterLoc.QuestsLoc.QuestClaimBtn, quest);
            if (claimButton.IsClickable()) buttons.Add(claimButton);
        }

        return buttons;
    }

    /// <summary>
    ///     Opens the Daily quests tab, reads the named quest's "done/target" progress, claims it if it's
    ///     ready, and closes the screen. Listed is false when the quest isn't there or the screen didn't
    ///     open; Done and Target are -1 when the progress text doesn't read.
    /// </summary>
    public static IEnumerator CheckDailyQuest(string name, Action<(bool Listed, int Done, int Target)> onRead)
    {
        yield return Open();
        yield return OpenQuestsTab;
        yield return OpenDailyQuestsSubTab;

        var result = (Listed: false, Done: -1, Target: -1);
        foreach (var quest in new GameElement(Paths.MenusLoc.CharacterLoc.QuestsLoc.DailyQuestsGridRoot).GetChildren())
        {
            if (new GameText(Paths.MenusLoc.CharacterLoc.QuestsLoc.QuestNameTxt, quest).GetParsedText() != name) continue;

            var progress = new GameText(Paths.MenusLoc.CharacterLoc.QuestsLoc.QuestProgressTxt, quest);
            result = (true, progress.GetParsedLeadingInt(-1), progress.GetParsedTrailingInt(-1));

            var claimButton = new GameButton(Paths.MenusLoc.CharacterLoc.QuestsLoc.QuestClaimBtn, quest);
            if (claimButton.IsClickable()) yield return claimButton.Click();
            break;
        }

        yield return Close;
        onRead(result);
    }
}
