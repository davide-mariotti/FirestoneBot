using System;
using System.Collections;
using Firebot.Core;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Alchemist;

public class Experiments : GameElement
{
    private const string Type = "alchExperimentType";
    private const string Slot = "alchExperimentSlot";

    // All 3 slots exist regardless of which ones the user has opted into STARTING via config's
    // resource_type (see ExperimentsTask) - a slot can be mid-experiment (or sitting completed)
    // from before that setting was narrowed, or just because all 3 are always visible in the UI.
    private static readonly string[] AllResourceIds = { "0", "1", "2" };

    public Experiments() : base(Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.Root) { }

    /// <summary>
    ///     Claims ANY completed experiment across all 3 slots. Deliberately NOT filtered by the
    ///     config's opted-in resource_type like Start() below - claiming an already-finished
    ///     experiment's reward is free, unlike starting a new one, so gating it behind the same
    ///     "these are real limited resources, opt in explicitly to spend them" opt-in silently
    ///     stranded completed experiments unclaimed on any account that hadn't opted every resource
    ///     in (live-confirmed, 2026-09-21: Steam-0 had 2 completed experiments sitting unclaimed with
    ///     resource_type empty).
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

    public IEnumerator Start(string[] experimentResources)
    {
        foreach (var resource in experimentResources)
        {
            var gePath = $"/{Slot}{resource}";
            var experimentSlot = new GameElement(gePath, this);
            if (experimentSlot.IsVisible()) continue; // Already active for this resource.

            var path = $"/{Type}{resource}/{Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.StartBtn}";
            var gameButton = new GameButton(path, this);
            if (gameButton.IsClickable()) yield return gameButton.Click();
        }
    }

    /// <summary>
    ///     Scans all 3 slots (not just the opted-in resource_type ones - see Claim()) for whichever
    ///     is currently running and due soonest, so a resource the user hasn't opted into STARTING
    ///     but that's already running from before (or that Claim() above just picked up mid-run) still
    ///     gets a correctly-scheduled recheck instead of being invisible to this task's own clock.
    ///     Only currently-active slots have a real timer to read - an inactive one's GameText.Time
    ///     would just read as DateTime.MinValue (nothing visible to parse) and wrongly win as "soonest".
    /// </summary>
    public DateTime NextRunTime()
    {
        var minTime = DateTime.MaxValue;
        foreach (var resource in AllResourceIds)
        {
            var slotPath = $"/{Slot}{resource}";
            if (!new GameElement(slotPath, this).IsVisible()) continue; // Not currently running.

            var path = $"{slotPath}/{Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.NextRunTimeTxt}";
            var time = new GameText(path, this).Time.AddSeconds(-BotSettings.FreeSpeedupSeconds);
            if (time > DateTime.Now && time < minTime) minTime = time;
        }

        // Nothing running (or nothing with a readable timer) - same 1h fallback as before rather
        // than an immediate retry loop.
        return minTime == DateTime.MaxValue ? DateTime.Now.AddHours(1) : minTime;
    }
}
