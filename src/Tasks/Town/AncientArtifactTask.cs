using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Creates an Ancient Artifact in the Tavern whenever the 5 elements for one are collected (the
///     bell on "Create artifact" is lit, and the AncientArtifact badge with it). Costs nothing else.
/// </summary>
public class AncientArtifactTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 15;

    // Elements come from Tavern draws, about 10 a day (GamerQuestTask).
    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    private const int MaxCreations = 10;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.AncientArtifact;

    public override IEnumerator Execute()
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenTavern;

        // One click creates it, no confirmation: on Steam-9 (18/10) the bell and the badge went off
        // at once, and the game showed its Achievements menu.
        var created = 0;
        while (created < MaxCreations && Tavern.CanCreateArtifact)
        {
            yield return Tavern.CreateArtifactBtn.Click();
            yield return Poll.Until(() => !Tavern.CanCreateArtifact);
            if (Tavern.CanCreateArtifact) break;
            created++;
        }

        Debug($"[INFO] Ancient Artifact: created {created}, bell still lit={Tavern.CanCreateArtifact}.");

        yield return Tavern.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
