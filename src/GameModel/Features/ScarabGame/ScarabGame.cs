using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using UnityEngine;

namespace Firebot.GameModel.Features.ScarabGame;

public static class ScarabGame
{
    // Neither the slot spin nor the vault reveal has been tested live before - same lesson as
    // chest-opening/Tavern/Awakening: their result animation likely runs well past the standard
    // interaction_delay, so poll for the button to actually become clickable again instead of
    // guessing a fixed delay (see ChestOpening.ChestTransitionPollWait for the same pattern).
    private static readonly WaitForSeconds AnimationPollWait = new(0.3f);
    private const int MaxAnimationPolls = 25; // ~7.5s ceiling

    public static IEnumerator OpenShop => new GameButton(Paths.ScarabGameLoc.OpenShopBtn).Click();

    public static IEnumerator OpenVault => new GameButton(Paths.ScarabGameLoc.OpenVaultBtn).Click();

    public static IEnumerator OpenMilestones => new GameButton(Paths.ScarabGameLoc.OpenMilestonesBtn).Click();

    public static GameButton SpinBtn => new(Paths.ScarabGameLoc.SpinBtn);

    /// <summary>Clicks the bet-multiplier cycle button until it shows the biggest option (best-effort -
    /// stops early if the button stops responding, since the exact number/labels of options and
    /// whether it wraps or clamps aren't verified live).</summary>
    public static IEnumerator MaxOutBet() =>
        CycleToMax(new GameButton(Paths.ScarabGameLoc.ChangeBetBtn), new GameText(Paths.ScarabGameLoc.BetQuantityTxt));

    /// <summary>Spins repeatedly (free Noble Tokens first, per the user - safe to click purely via
    /// IsClickable()) until unaffordable, waiting out each spin's reveal animation in between so the
    /// next spin doesn't cut it short.</summary>
    public static IEnumerator SpinUntilExhausted()
    {
        while (SpinBtn.IsClickable())
        {
            yield return SpinBtn.Click();
            yield return WaitUntilClickable(SpinBtn);
        }
    }

    public static IEnumerator Close => new GameButton(Paths.ScarabGameLoc.CloseBtn).Click();

    /// <summary>Shared by ScarabGame's bet selector and PharaohsVault's quantity selector - both are a
    /// single button that cycles a multiplier, shown in a sibling text label. Bounded attempts since
    /// the exact option count/labels aren't confirmed live.</summary>
    internal static IEnumerator CycleToMax(GameButton cycleBtn, GameText quantityTxt)
    {
        const int maxAttempts = 4;

        for (var i = 0; i < maxAttempts; i++)
        {
            if (quantityTxt.GetParsedText().Contains("10")) yield break;
            if (!cycleBtn.IsClickable()) yield break;
            yield return cycleBtn.Click();
        }
    }

    internal static IEnumerator WaitUntilClickable(GameButton button)
    {
        var pollsLeft = MaxAnimationPolls;
        while (pollsLeft > 0 && !button.IsClickable())
        {
            yield return AnimationPollWait;
            pollsLeft--;
        }
    }
}

public static class ScarabGameShop
{
    public static IEnumerator OpenSaleTab => new GameButton(Paths.ScarabGameShopLoc.SaleTabBtn).Click();

    public static IEnumerator ClaimFreeToken => new GameButton(Paths.ScarabGameShopLoc.FreeTokenLoc.ClaimBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.ScarabGameShopLoc.CloseBtn).Click();
}

/// <summary>Opened by ScarabGame.OpenVault - spends Ancient Coins (5000 per the wiki) for 5 random
/// rewards, including jewel/celestial chests (opened separately by CollectorQuestTask) and Sigils of
/// Prophecy (used to release Beasts - a separate system, not automated here).</summary>
public static class PharaohsVault
{
    public static GameButton OpenBtn => new(Paths.PharaohsVaultLoc.OpenBtn);

    public static IEnumerator MaxOutQuantity() => ScarabGame.CycleToMax(
        new GameButton(Paths.PharaohsVaultLoc.ChangeQuantityBtn), new GameText(Paths.PharaohsVaultLoc.QuantityTxt));

    /// <summary>Opens repeatedly until unaffordable, waiting out each open's reveal animation in
    /// between so the next open doesn't cut it short - same lesson as ScarabGame.SpinUntilExhausted.</summary>
    public static IEnumerator OpenUntilExhausted()
    {
        while (OpenBtn.IsClickable())
        {
            yield return OpenBtn.Click();
            yield return ScarabGame.WaitUntilClickable(OpenBtn);
        }
    }

    public static IEnumerator Close => new GameButton(Paths.PharaohsVaultLoc.CloseBtn).Click();
}

/// <summary>Opened by ScarabGame.OpenMilestones - a Scarab-level XP reward track, folded into
/// PharaohsVaultTask per the user's request (2026-09-20). Fully live-confirmed across several
/// rounds of diagnostics, 2026-09-20 - see Paths.ScarabGameMilestonesLoc for the real structure.</summary>
public static class ScarabGameMilestones
{
    public static bool IsVisible => new GameElement(Paths.ScarabGameMilestonesLoc.Root).IsVisible();

    /// <summary>The 20 real tier nodes, filtered out of "milestones"' 23 children (which also
    /// include 3 decorative siblings - MilestoneProgressBarBg/Starting/Ending, none of them a real
    /// tier) - each properly indexed ("ScarabMilestone", "ScarabMilestone (1)".."(19)"), unlike
    /// Pirate's Prize's tier list this isn't the duplicate-sibling-name situation.</summary>
    private static IEnumerable<GameElement> Tiers =>
        new GameElement(Paths.ScarabGameMilestonesLoc.MilestonesRoot).GetChildren()
            .Where(t => t.Name.StartsWith("ScarabMilestone"));

    /// <summary>Claims every tier whose claimButton is currently clickable - exactly one of
    /// claimedOverlay/locked/claimButton is active per tier depending on its state (already
    /// claimed / not yet reached / ready now), so this naturally skips already-claimed and
    /// not-yet-reached tiers and only acts on whichever one(s) are genuinely ready. Loops the whole
    /// list more than once in case claiming one tier's XP milestone immediately unlocks the next.</summary>
    public static IEnumerator ClaimUntilExhausted()
    {
        const int maxPasses = 3; // generous ceiling - claiming rarely cascades more than one tier at once

        for (var pass = 0; pass < maxPasses; pass++)
        {
            var claimedAny = false;

            foreach (var tier in Tiers)
            {
                var claimBtn = new GameButton(Paths.ScarabGameMilestonesLoc.ClaimBtn(tier.Name));
                if (!claimBtn.IsClickable()) continue;

                yield return claimBtn.Click();
                claimedAny = true;
            }

            if (!claimedAny) yield break;
        }
    }

    public static IEnumerator Close => new GameButton(Paths.ScarabGameMilestonesLoc.CloseBtn).Click();
}
