using System;
using System.Collections;
using System.Collections.Generic;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
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

    // The arena's own count (ArenaOfKingsMech, set while the arena is open), the "current/max" text as the fallback.
    public static int TokensAvailable => GameState.Get("arena tokens", () => Il2Cpp.GameInitialize.MenuLoader.ArenaOfKingsMech.battleTokensCurrent)
                                         ?? new GameText(Paths.ArenaOfKingsLoc.BattleTokensTxt).GetParsedLeadingInt();

    // The game's own figure for the arena formation (WarMachineHandler), the text as the fallback.
    public static double MyPower =>
        GameState.Get("my arena power", () => GameState.H.WarMachineHandler.myFormationArenaPower) is > 0 and var power
            ? power
            : new GameText(Paths.ArenaOfKingsLoc.MyArenaPowerTxt).GetParsedDoubleAbbreviated();

    public static GameButton RerollBtn => new(Paths.ArenaOfKingsLoc.RerollBtn);

    public static IEnumerator Reroll => RerollBtn.Click();

    private static GameElement OpponentGrid => new(Paths.ArenaOfKingsLoc.OpponentGridRoot);

    /// <summary>
    ///     The power of each opponent, in slot order: the game's own number on the slot's opponent
    ///     (exact, and never the blank the text reads right after a reroll), else the text.
    /// </summary>
    public static IReadOnlyList<double> OpponentPowers()
    {
        var powers = new List<double>(OpponentCount);
        for (var i = 0; i < OpponentCount; i++)
        {
            var opponent = OpponentGrid.GetChild(i);
            var text = new GameText(Paths.ArenaOfKingsLoc.OpponentPowerTxt, opponent).GetParsedDoubleAbbreviated();
            var game = GameState.Get("arena opponent power", () =>
                GameElement.FindTransform(Paths.ArenaOfKingsLoc.OpponentGridRoot).GetChild(i)
                    .GetComponent<Il2Cpp.ArenaOfKingsOpponentInteraction>().opponent.warMachineArenaPower);
            // The text is abbreviated ("39.5K"): only a gap past its rounding counts.
            if (game is { } g && text > 0 && Math.Abs(g - text) > Math.Max(1, g * 0.01))
                GameState.Compare($"arena opponent {i} power", $"{g:0}", $"{text:0}");
            powers.Add(game is > 0 ? game.Value : text);
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
