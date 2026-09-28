using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town;

/// <summary>The hub Town.OpenBattles opens; only its arena option is used.</summary>
public static class WFMenuSelection
{
    public static IEnumerator OpenArena => new GameButton(Paths.WFMenuSelectionLoc.ArenaBtn).Click();
}

public static class ArenaOfKings
{
    private const int OpponentCount = 3;

    // "current/max", e.g. "5/5".
    public static int TokensAvailable => new GameText(Paths.ArenaOfKingsLoc.BattleTokensTxt).GetParsedLeadingInt();

    public static double MyPower =>
        new GameText(Paths.ArenaOfKingsLoc.MyArenaPowerTxt).GetParsedDoubleAbbreviated();

    public static GameButton RerollBtn => new(Paths.ArenaOfKingsLoc.RerollBtn);

    public static IEnumerator Reroll => RerollBtn.Click();

    private static GameElement OpponentGrid => new(Paths.ArenaOfKingsLoc.OpponentGridRoot);

    /// <summary>The power of each opponent, in slot order.</summary>
    public static IReadOnlyList<double> OpponentPowers()
    {
        var powers = new List<double>(OpponentCount);
        for (var i = 0; i < OpponentCount; i++)
        {
            var opponent = OpponentGrid.GetChild(i);
            powers.Add(new GameText(Paths.ArenaOfKingsLoc.OpponentPowerTxt, opponent).GetParsedDoubleAbbreviated());
        }

        return powers;
    }

    public static IEnumerator Fight(int slotIndex)
    {
        var opponent = OpponentGrid.GetChild(slotIndex);
        yield return new GameButton(Paths.ArenaOfKingsLoc.OpponentFightBtn, opponent).Click();
    }

    public static IEnumerator Close => new GameButton(Paths.ArenaOfKingsLoc.CloseBtn).Click();
}

/// <summary>The formation preview; only its start button is pressed.</summary>
public static class AOKBattlePreview
{
    public static IEnumerator Fight => new GameButton(Paths.AOKBattlePreviewLoc.FightBtn).Click();
}

public static class AOKBattleResult
{
    public static bool IsVisible => new GameElement(Paths.AOKBattleResultLoc.CloseBtn).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.AOKBattleResultLoc.CloseBtn).Click();
}
