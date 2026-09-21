using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using PharaohsVaultScreen = Firebot.GameModel.Features.ScarabGame.PharaohsVault;
using ScarabGameScreen = Firebot.GameModel.Features.ScarabGame.ScarabGame;
using ScarabGameMilestonesScreen = Firebot.GameModel.Features.ScarabGame.ScarabGameMilestones;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.ScarabGame;

/// <summary>
///     Spins Scarab's Game slot machine with free Noble Tokens (10/day) and spends the resulting
///     Ancient Coins to open Pharaoh's Vault (5000 coins per open, per the wiki), then also claims the
///     Scarab-level Milestones reward track (per the user's screenshot, 2026-09-20 - folded in here
///     rather than a separate task, since it's reached from the same Scarab's Game screen). Separate
///     from ScarabGameFreeTokenTask (which only claims an unrelated free shop item).
///     Always uses the biggest available bet/quantity multiplier on both the spin and the vault open
///     (per the user - a proportional bet/payout, so it's strictly fewer clicks for the same result).
///     Confirmed by the user: the spin button always draws from free Noble Tokens first and simply
///     becomes non-clickable once they're exhausted - it never silently spends Pharaoh Tokens (bought
///     with gems), so clicking purely via IsClickable() is safe here, same as everywhere else.
///     Vault rewards can include jewel/celestial chests (opened separately by CollectorQuestTask,
///     extended for this) and Sigils of Prophecy (used to release Beasts - a separate system, out of
///     scope for now per the user).
///     Level-gated like ScarabGameFreeTokenTask - the wiki's Scarab's Game infobox lists "unlock =
///     Level 60".
/// </summary>
public class PharaohsVaultTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.ScarabGame;
    protected override int MinimumCharacterLevel => 60;

    // Corrected, 2026-09-20: the "PharaohsVault" badge (first guess) is real but never actually lit
    // in the live rail dump - the user pointed out they were seeing a real Pharaoh's-Vault-related
    // notification on screen that this task wasn't reacting to. The badge that's actually lit is the
    // generic "ScarabGame" one (confirmed true in the rail dump right when the user reported this) -
    // makes sense, since it's presumably "free Scarab's Game spins are ready" (Noble Tokens recharge
    // to a fixed daily amount per the wiki), which is exactly this task's own trigger condition, not
    // a fuzzy threshold gate. Note ScarabGameFreeTokenTask also opportunistically tries this same
    // badge (a different, unrelated shop freebie in the same screen) but isn't scheduled on it - its
    // own "ScarabGameShopFreeToken" badge is the more precise signal for that task specifically.
    // ScarabGameMilestones (added 2026-09-20, per the user) is a second, separate real badge for the
    // Milestones track below - NotificationBadgeName only takes one bare name, so both are covered
    // via NotificationPathCandidates instead (each on both HUD rail variants).
    protected override string[] NotificationPathCandidates => new[]
    {
        Paths.BattleLoc.NotificationsLoc.Root + "/" + Paths.BattleLoc.NotificationsLoc.ScarabGame,
        Paths.BattleLoc.NotificationsLoc.FallbackRoot + "/" + Paths.BattleLoc.NotificationsLoc.ScarabGame,
        Paths.BattleLoc.NotificationsLoc.Root + "/" + Paths.BattleLoc.NotificationsLoc.ScarabGameMilestones,
        Paths.BattleLoc.NotificationsLoc.FallbackRoot + "/" + Paths.BattleLoc.NotificationsLoc.ScarabGameMilestones
    };

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    public override IEnumerator Execute()
    {
        // Fast path - opportunistic only, safe no-op if not up (see NotificationPathCandidates above).
        yield return Notifications.ScarabGame;
        yield return Notifications.ScarabGameMilestones;

        yield return TownScreen.Open;
        yield return TownScreen.OpenScarabGame;

        yield return ScarabGameScreen.MaxOutBet();
        yield return ScarabGameScreen.SpinUntilExhausted();

        yield return ScarabGameScreen.OpenVault;
        yield return PharaohsVaultScreen.MaxOutQuantity();
        yield return PharaohsVaultScreen.OpenUntilExhausted();
        yield return PharaohsVaultScreen.Close;

        yield return ScarabGameScreen.OpenMilestones;

        if (ScarabGameMilestonesScreen.IsVisible)
        {
            yield return ScarabGameMilestonesScreen.ClaimUntilExhausted();
            yield return ScarabGameMilestonesScreen.Close;
        }
        else
        {
            Debug("Scarab Game Milestones not visible after navigating.");
        }

        yield return ScarabGameScreen.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
