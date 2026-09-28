using System;
using System.Collections;
using Firebot.Core;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Alchemist;

/// <summary>The Alchemist's three experiment slots, one per resource (0 Dragon blood, 1 Strange dust, 2 Exotic coin).</summary>
public class Experiments : GameElement
{
    private const string Type = "alchExperimentType";
    private const string Slot = "alchExperimentSlot";

    private static readonly string[] AllResourceIds = { "0", "1", "2" };

    public Experiments() : base(Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.Root) { }

    /// <summary>
    ///     Collects every finished experiment in all three slots, not just the configured resources:
    ///     collecting is free, and a slot can still be running from before the setting changed.
    /// </summary>
    public IEnumerator Claim()
    {
        foreach (var resource in AllResourceIds)
        {
            var speedupFinishDesc = $"/{Slot}{resource}/{Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.SpeedupFinishDesc}";
            var speedupFinish = new GameElement(speedupFinishDesc, this);
            if (!speedupFinish.IsVisible())
            {
                var speedBtnPath = $"/{Slot}{resource}/{Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.SpeedupBtn}";
                var button = new GameButton(speedBtnPath, this);
                if (button.IsClickable()) yield return button.Click();
            }

            var claimBtnPath = $"/{Slot}{resource}/{Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.ClaimBtn}";
            var gameButton = new GameButton(claimBtnPath, this);
            if (gameButton.IsClickable()) yield return gameButton.Click();
        }
    }

    /// <summary>Starts an experiment for each given resource whose slot isn't already running.</summary>
    public IEnumerator Start(string[] experimentResources)
    {
        foreach (var resource in experimentResources)
        {
            var gePath = $"/{Slot}{resource}";
            var experimentSlot = new GameElement(gePath, this);
            if (experimentSlot.IsVisible()) continue;

            var path = $"/{Type}{resource}/{Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.StartBtn}";
            var gameButton = new GameButton(path, this);
            if (gameButton.IsClickable()) yield return gameButton.Click();
        }
    }

    /// <summary>
    ///     When the soonest running experiment (any slot) enters the free speed-up window, or an hour
    ///     from now if none is running. Only running slots count: an idle one has no timer to read.
    /// </summary>
    public DateTime NextRunTime()
    {
        var minTime = DateTime.MaxValue;
        foreach (var resource in AllResourceIds)
        {
            var slotPath = $"/{Slot}{resource}";
            if (!new GameElement(slotPath, this).IsVisible()) continue;

            var path = $"{slotPath}/{Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.NextRunTimeTxt}";
            var time = new GameText(path, this).Time.AddSeconds(-BotSettings.FreeSpeedupSeconds);
            if (time > DateTime.Now && time < minTime) minTime = time;
        }

        return minTime == DateTime.MaxValue ? DateTime.Now.AddHours(1) : minTime;
    }
}
