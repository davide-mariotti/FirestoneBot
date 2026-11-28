using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using PharaohsVaultScreen = Firebot.GameModel.Features.ScarabGame.PharaohsVault;
using ScarabGameScreen = Firebot.GameModel.Features.ScarabGame.ScarabGame;
using ScarabGameMilestonesScreen = Firebot.GameModel.Features.ScarabGame.ScarabGameMilestones;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.ScarabGame;

/// <summary>
///     Scarab's Game: spins with the free Noble Tokens, spends the Ancient Coins won on Pharaoh's
///     Vault, then claims the Scarab level milestones. Bet and vault quantity are pushed to x10 where
///     the toggle gets there: both are proportional, so it only saves clicks. The spin button never
///     touches the bought Pharaoh Tokens. The Sigils of Prophecy the vault gives aren't used.
/// </summary>
public class PharaohsVaultTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.ScarabGame;
    protected override int MinimumCharacterLevel => 60;

    // ScarabGame lights for the free spins, ScarabGameMilestones for the milestone track, PharaohsVault
    // for the coins a milestone hands out after this run's vault step.
    protected override string[] NotificationPaths =>
        RailBadges(Paths.BattleLoc.NotificationsLoc.ScarabGame, Paths.BattleLoc.NotificationsLoc.ScarabGameMilestones,
            Paths.BattleLoc.NotificationsLoc.PharaohsVault);

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(6);

    public override IEnumerator Execute()
    {
        // No badge, no token to spin and the coins short of a vault: the screens hold nothing (28 s a run).
        var tokens = GameState.Currency(Il2Cpp.CurrencyCode.NoblesToken) + GameState.Currency(Il2Cpp.CurrencyCode.PharaohsToken);
        var coins = GameState.Currency(Il2Cpp.CurrencyCode.AncientCoin);
        var vault = GameState.Get("pharaoh's vault cost", () => Il2Cpp.ScarabGameHandler.pharaohsVaultCost);
        if (!IsNotificationVisible() && tokens < 1 && coins < vault)
        {
            Debug($"[INFO] From the game: {tokens:0} tokens to spin, {coins:0} Ancient Coins of {vault} a vault - not opening.");
            NextRunTime = DateTime.Now + RecheckDelay;
            yield break;
        }

        // The vault's coins reach a milestone and a milestone's coins buy another vault, each lit only
        // after its step: one more round for them, as the next run would come 30 minutes later
        // (28/11: ScarabGameMilestones lit after the 10:00 runs on 20-24 and 27, PharaohsVault on 26, 28).
        for (var round = 1; round <= MaxRounds; round++)
        {
            if (round > 1 && GameState.HasBadge(Il2Cpp.NotificationType.ScarabGameMilestones) != true &&
                !(GameState.Currency(Il2Cpp.CurrencyCode.AncientCoin) >= vault))
                break;

            if (round > 1) Debug($"[INFO] Scarab round {round}: a milestone or a vault the last round lit.");
            yield return Round();
        }

        NextRunTime = DateTime.Now + RecheckDelay;
    }

    private const int MaxRounds = 3;

    private static IEnumerator Round()
    {
        yield return Notifications.ScarabGame;
        yield return Notifications.ScarabGameMilestones;

        yield return TownScreen.Open;
        yield return TownScreen.OpenScarabGame;

        yield return ScarabGameScreen.MaxOutBet();
        yield return ScarabGameScreen.SpinUntilExhausted();

        yield return ScarabGameScreen.OpenVault;
        yield return Poll.Until(() => PharaohsVaultScreen.OpenBtn.IsVisible(), 20);
        yield return PharaohsVaultScreen.MaxOutQuantity();
        yield return PharaohsVaultScreen.OpenUntilExhausted();
        yield return PharaohsVaultScreen.Close;

        // Scarab's Game has no entry to its milestone track: only the rail badge clicked above opens it.
        if (ScarabGameMilestonesScreen.IsVisible)
        {
            yield return ScarabGameMilestonesScreen.ClaimUntilExhausted();
            yield return ScarabGameMilestonesScreen.Close;
        }

        yield return ScarabGameScreen.Close;
        yield return TownScreen.Close;
    }
}
