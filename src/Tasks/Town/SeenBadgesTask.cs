using System;
using System.Collections;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Base;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Logger = Firebot.Core.Logger;

namespace Firebot.Tasks.Town;

/// <summary>
///     The rail badges that only ask for their screen to be seen (the game's NotificationHandler keeps
///     an arcaneCrystalSeen and a meteoriteReseachSeen), lit again by every restart since the flags
///     live in memory. Clicks each one and closes what it opens: spends nothing.
/// </summary>
public class SeenBadgesTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private static readonly (string Name, Func<IEnumerator> Click)[] Badges =
    {
        (Paths.BattleLoc.NotificationsLoc.ArcaneCrystal, () => Notifications.ArcaneCrystal),
        (Paths.BattleLoc.NotificationsLoc.MeteoriteResearch, () => Notifications.MeteoriteResearch)
    };

    protected override string[] NotificationPaths => RailBadges(Badges.Select(b => b.Name).ToArray());

    public override IEnumerator Execute()
    {
        foreach (var (name, click) in Badges)
        {
            if (!new GameElement(Paths.BattleLoc.NotificationsLoc.Root + "/" + name).IsVisible()) continue;

            yield return click();
            yield return Watchdog.ForceClearAll();
            Logger.Debug($"[SeenBadgesTask] [INFO] {name} seen, now lit: {Notifications.Lit}.");
        }

        // Only its badges bring it back.
        NextRunTime = DateTime.MaxValue;
    }
}
