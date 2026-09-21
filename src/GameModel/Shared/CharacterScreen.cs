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
    ///     Live-confirmed, 2026-09-20 (TalentsTask): a single click on the avatar icon doesn't reliably
    ///     open the Character popup when called very soon after the game/scene finishes loading (e.g.
    ///     the first task to run right after mod start) - no error ever logged either way, the click
    ///     just silently has no effect that first time, succeeding a couple of seconds later on a
    ///     retry once the screen's own UI has caught up. Checks IsVisible before/after each attempt so
    ///     it's a safe no-op once genuinely open, and never re-clicks (which could instead CLOSE it).
    /// </summary>
    public static IEnumerator Open()
    {
        for (var attempt = 0; attempt < 5 && !IsOpen; attempt++)
        {
            yield return new GameButton(Paths.BattleLoc.PlayerAvatarLoc.OpenBtn).Click();
            if (!IsOpen) yield return new WaitForSeconds(2f);
        }
    }

    /// <summary>Whether the Character popup is genuinely open right now - check this after Open() if
    /// what follows would misbehave against a screen that never actually opened (e.g. TalentsTask's
    /// guide calibration, which must never run against unresolved/misread data).</summary>
    public static bool IsOpen => new GameElement(Paths.MenusLoc.CharacterLoc.Root).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.CharacterLoc.CloseBtn).Click();

    public static IEnumerator OpenQuestsTab => new GameButton(Paths.MenusLoc.CharacterLoc.QuestsTabBtn).Click();

    public static IEnumerator OpenTalentsTab => new GameButton(Paths.MenusLoc.CharacterLoc.TalentsTabBtn).Click();

    public static IEnumerator OpenDailyQuestsSubTab =>
        new GameButton(Paths.MenusLoc.CharacterLoc.QuestsLoc.DailyTabBtn).Click();

    public static IEnumerator OpenWeeklyQuestsSubTab =>
        new GameButton(Paths.MenusLoc.CharacterLoc.QuestsLoc.WeeklyTabBtn).Click();

    /// <summary>Shared element - read it right after selecting the tab whose countdown you want.</summary>
    public static DateTime QuestsRenewTime => new GameText(Paths.MenusLoc.CharacterLoc.QuestsLoc.RenewTxt).Time;

    public static List<GameButton> DailyQuestClaimButtons() =>
        QuestClaimButtons(Paths.MenusLoc.CharacterLoc.QuestsLoc.DailyQuestsGridRoot);

    public static List<GameButton> WeeklyQuestClaimButtons() =>
        QuestClaimButtons(Paths.MenusLoc.CharacterLoc.QuestsLoc.WeeklyQuestsGridRoot);

    /// <summary>
    ///     Every quest slot's claim button in the given grid - clicking one that isn't actually
    ///     completable yet is a safe no-op (same as every other button in this codebase), so callers
    ///     can just click all of them without checking completion state first.
    /// </summary>
    private static List<GameButton> QuestClaimButtons(string gridRootPath)
    {
        var grid = new GameElement(gridRootPath);
        var buttons = new List<GameButton>();
        foreach (var quest in grid.GetChildren())
            buttons.Add(new GameButton("claimButton", quest));
        return buttons;
    }
}
