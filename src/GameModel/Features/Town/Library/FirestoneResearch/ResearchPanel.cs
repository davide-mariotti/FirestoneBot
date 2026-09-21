using System;
using System.Collections;
using Firebot.Core;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Town.Library.FirestoneResearch;

public class ResearchPanel : GameElement
{
    public ResearchPanel() : base(Paths.MenusLoc.LibraryLoc.ResearchPanelLoc.Root) { }

    public static bool HasEmptySlot =>
        new GameElement(Paths.MenusLoc.LibraryLoc.ResearchPanelLoc.SelectResearchTable).IsVisible();

    public IEnumerator Claim()
    {
        foreach (var child in GetChildren())
            if (child.IsVisible() && child.Name.StartsWith("researchSlot"))
            {
                var speedBtn = new GameButton(Paths.MenusLoc.LibraryLoc.ResearchPanelLoc.SpeedupBtn, child);

                var canClaim = !new GameElement(
                    Paths.MenusLoc.LibraryLoc.ResearchPanelLoc.SpeedupFinishDesc, child).IsVisible();

                if (speedBtn.IsVisible() && canClaim)
                {
                    Debug("[INFO] Claiming completed research.");
                    yield return speedBtn.Click();
                    yield break;
                }

                yield return new GameButton(Paths.MenusLoc.LibraryLoc.ResearchPanelLoc.ClaimBtn, child).Click();
            }
    }

    public DateTime NextRunTime()
    {
        var minTime = DateTime.MaxValue;
        foreach (var child in GetChildren())
            if (child.IsVisible() && child.Name.StartsWith("researchSlot"))
            {
                var time = new GameText(Paths.MenusLoc.LibraryLoc.ResearchPanelLoc.NextRunTimeTxt, child).Time
                    .AddSeconds(-BotSettings.FreeSpeedupSeconds);

                if (time < minTime) minTime = time;
            }

        return minTime == DateTime.MaxValue ? DateTime.MinValue : minTime;
    }
}
