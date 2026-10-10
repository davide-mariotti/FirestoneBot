using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Firebot.Utilities;

namespace Firebot.GameModel.Features.Town.Oracle;

public class Rituals : GameElement
{
    public Rituals() : base(Paths.MenusLoc.OracleLoc.RitualLoc.Rituals) { }

    public int Claimed { get; private set; }

    public int Started { get; private set; }

    public IEnumerator Claim()
    {
        foreach (var button in ClickableButtons(Paths.MenusLoc.OracleLoc.RitualLoc.ClaimBtn))
        {
            yield return button.Click();
            Claimed++;
        }
    }

    public IEnumerator Start()
    {
        foreach (var button in ClickableButtons(Paths.MenusLoc.OracleLoc.RitualLoc.StartBtn))
        {
            yield return button.Click();
            Started++;
        }
    }

    // Lazy, so each button is checked after the click before it: starting one ritual disables the
    // others' start (10/10, Steam-0).
    private IEnumerable<GameButton> ClickableButtons(string path) => GetChildren()
        .Where(child => child.IsVisible())
        .Select(child => new GameButton(path, child))
        .Where(button => button.IsClickable());

    public DateTime CurrentRunTime()
    {
        DateTime? earliest = null;

        foreach (var child in GetChildren().Where(child => child.IsVisible()))
        {
            var dateTime = new GameText(Paths.MenusLoc.OracleLoc.RitualLoc.CurrentRunTimeTxt, child);
            if (!dateTime.IsVisible()) continue;
            earliest = dateTime.Time;
            break;
        }

        return earliest ?? NextRunTime();
    }

    private static DateTime NextRunTime()
    {
        var text = new GameText(Paths.MenusLoc.OracleLoc.RitualLoc.NextRunTimeTxt).GetParsedText();
        return TimeParser.ParseExpectedTime(text);
    }
}
