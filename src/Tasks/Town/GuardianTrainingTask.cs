using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.Town.MagicQuarters;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Keeps a guardian training in Magic Quarters. Trains guardian_index when that guardian is
///     unlocked, guardian 0 (Vermilion, the dragon) otherwise - the one the F2P guide wants trained
///     for its gold bonus.
/// </summary>
public class GuardianTrainingTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private MelonPreferences_Entry<int> _guardianIndex;
    private MelonPreferences_Entry<bool> _useStrangeDust;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.GuardianTraining;

    public override IEnumerator Execute()
    {
        yield return Notifications.GuardianTraining;

        yield return TownScreen.Open;
        yield return TownScreen.OpenMagicQuarters;

        var preferredIndex = GetGuardianIndex();
        var guardianIndex = IsAvailable(preferredIndex) ? preferredIndex : 0;

        var guardianBtn = new GameButton(parent: MagicQuarters.Guardians.GetChild(guardianIndex));
        yield return TryTrainGuardian(guardianBtn, MagicQuarters.TrainBtn, _useStrangeDust?.Value ?? false);

        NextRunTime = MagicQuarters.NextRunTime;

        yield return MagicQuarters.CloseLockedPopup;
        yield return MagicQuarters.Close;
        yield return TownScreen.Close;
    }

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_guardianIndex != null) return;

        _guardianIndex = category.CreateEntry(
            "guardian_index",
            0,
            "Guardian Index",
            "Select guardian index for training. Use 0-3. Default is 0.\n" +
            "0=Vermilion, 1=Grace, 2=Ankaa, 3=Azhar"
        );

        _useStrangeDust = category.CreateEntry(
            "use_strange_dust",
            false,
            "Use Strange Dust",
            "Use 'Strange Dust' for training. Default is false."
        );
    }

    private static bool IsAvailable(int index)
    {
        var guardian = new Guardian(parent: MagicQuarters.Guardians.GetChild(index));
        return guardian.IsVisible() && guardian.IsUnlocked;
    }

    private IEnumerator TryTrainGuardian(GameButton guardianBtn, GameButton trainBtn, bool useStrangeDust)
    {
        yield return guardianBtn.Click();
        yield return trainBtn.Click();
        yield return ApplyEnlightenment(useStrangeDust);
    }

    private IEnumerator ApplyEnlightenment(bool useStrangeDust)
    {
        if (!useStrangeDust) yield break;

        var enlightenmentBtn = Guardian.EnlightenmentBtn;
        while (enlightenmentBtn.IsClickable()) yield return enlightenmentBtn.Click();
    }

    private int GetGuardianIndex()
    {
        var value = _guardianIndex?.Value ?? 0;

        if (value >= 0 && value <= 3) return value;

        Debug($"[FAILED] Invalid guardian_index '{value}'. Using default '0'.");
        return 0;
    }
}
