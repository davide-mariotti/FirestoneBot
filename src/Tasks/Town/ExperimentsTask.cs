using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.Alchemist;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;
using UnityEngine;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>Collects finished Alchemist experiments and starts new ones with the configured resources.</summary>
public class ExperimentsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;
    protected override int MinimumCharacterLevel => 120;

    private MelonPreferences_Entry<string> _resourceType;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.Experiments;

    public override IEnumerator Execute()
    {
        yield return Notifications.Experiments;

        yield return TownScreen.Open;
        yield return TownScreen.OpenAlchemist;

        // The game says when the Alchemist is done loading (the fixed 3 s wait before).
        yield return Poll.Until(() => GameState.Busy == false, 30, 0.1f);
        if (GameState.ExperimentsEnd is { } due && SpeedUpButton.FreeFrom(due) <= DateTime.Now)
            yield return Poll.Until(() => SpeedUpButton.AnySlotReady(Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.Root,
                "alchExperimentSlot", Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.SpeedupBtn,
                Paths.MenusLoc.AlchemistLoc.ExperimentsLoc.ClaimBtn), 50, 0.1f);
        var resources = GetResourceTypes();
        var experiments = new Experiments();
        // Collecting is free, so it isn't limited to resource_type - only starting one spends anything.
        yield return experiments.Claim();
        yield return new WaitForSeconds(1);
        yield return experiments.Start(resources);
        var fromScreen = experiments.NextRunTime();
        var fromGame = GameState.ExperimentsEnd is { } end ? SpeedUpButton.FreeFrom(end) : (DateTime?)null;
        GameState.CompareTimes("experiments next", fromGame, fromScreen);
        NextRunTime = fromGame ?? fromScreen;

        yield return Alchemist.Close;
        yield return TownScreen.Close;
    }

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_resourceType != null) return;

        _resourceType = category.CreateEntry(
            "resource_type",
            "0",
            "Experiment Resources",
            "ALCHEMIST EXPERIMENT RESOURCE CONFIGURATION. " +
            "\nThis setting controls which experiment resources are SPENT on starting experiments. " +
            "\nValid IDs: 0=Dragon blood, 1=Strange dust, 2=Exotic coin. " +
            "\nEnter comma-separated IDs (e.g. '0,1,2'). " +
            "\nAny value other than 0, 1, or 2 will be ignored. Set to empty to never start an " +
            "experiment at all (claiming finished ones is free and happens regardless). " +
            "\nDefault: '0' (Dragon blood only) - per the F2P guide, Dragon blood is the one currency " +
            "that belongs in experiments, and it comes back from the map's dragon missions. " +
            "\nDO NOT ADD 1 (Strange dust): the guide lists spending it here among the mistakes to " +
            "avoid - it's needed for tier 2 soul stones (1.000 per hero) and Guardian evolutions, and " +
            "an experiment gives a random result of which only about a third of the possible bonuses " +
            "are useful. " +
            "\nEXAMPLES: '0' = Dragon blood only (recommended). '0,2' = Dragon blood and Exotic coin."
        );
    }

    private string[] GetResourceTypes()
    {
        if (_resourceType == null)
        {
            Debug("[INFO] Missing resource_type entry. No resources set.");
            return Array.Empty<string>();
        }

        var value = _resourceType.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug("[INFO] Empty resource_type value. No resources set.");
            return Array.Empty<string>();
        }

        var validIds = new[] { "0", "1", "2" };
        var resources = value.Split(',')
            .Select(x => x.Trim())
            .Where(x => validIds.Contains(x))
            .ToArray();

        if (resources.Length != 0) return resources;

        Debug($"[FAILED] Invalid resource_type '{value}'. All values must be 0, 1, or 2. No resources set.");
        return Array.Empty<string>();
    }
}
