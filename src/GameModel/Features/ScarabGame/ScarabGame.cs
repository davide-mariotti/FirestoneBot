using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.ScarabGame;

public static class ScarabGame
{
    public static IEnumerator OpenShop => new GameButton(Paths.ScarabGameLoc.OpenShopBtn).Click();

    public static IEnumerator OpenVault => new GameButton(Paths.ScarabGameLoc.OpenVaultBtn).Click();

    public static IEnumerator OpenMilestones => new GameButton(Paths.ScarabGameLoc.OpenMilestonesBtn).Click();

    public static GameButton SpinBtn => new(Paths.ScarabGameLoc.SpinBtn);

    public static IEnumerator MaxOutBet() =>
        CycleToMax(new GameButton(Paths.ScarabGameLoc.ChangeBetBtn), new GameText(Paths.ScarabGameLoc.BetQuantityTxt));

    /// <summary>Spins until out of free tokens, letting each spin's reveal finish before the next.</summary>
    public static IEnumerator SpinUntilExhausted()
    {
        while (SpinBtn.IsClickable())
        {
            yield return SpinBtn.Click();
            yield return WaitUntilClickable(SpinBtn);
        }
    }

    public static IEnumerator Close => new GameButton(Paths.ScarabGameLoc.CloseBtn).Click();

    /// <summary>
    ///     Best effort: clicks a bet/quantity toggle until its label shows "10", at most 4 times, and
    ///     stops early if the toggle stops responding. The bet is proportional (same payout per token),
    ///     so the biggest one just saves clicks. The real option labels aren't confirmed.
    /// </summary>
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

    internal static IEnumerator WaitUntilClickable(GameButton button) =>
        Poll.Until(() => button.IsClickable(), 25, 0.3f);
}

public static class ScarabGameShop
{
    public static IEnumerator OpenSaleTab => new GameButton(Paths.ScarabGameShopLoc.SaleTabBtn).Click();

    public static IEnumerator ClaimFreeToken => new GameButton(Paths.ScarabGameShopLoc.FreeTokenLoc.ClaimBtn).Click();

    public static IEnumerator Close => new GameButton(Paths.ScarabGameShopLoc.CloseBtn).Click();
}

/// <summary>Spends Ancient Coins on random rewards, jewel and celestial chests among them.</summary>
public static class PharaohsVault
{
    public static GameButton OpenBtn => new(Paths.PharaohsVaultLoc.OpenBtn);

    public static IEnumerator MaxOutQuantity() => ScarabGame.CycleToMax(
        new GameButton(Paths.PharaohsVaultLoc.ChangeQuantityBtn), new GameText(Paths.PharaohsVaultLoc.QuantityTxt));

    /// <summary>Opens until the coins run out, letting each reveal finish before the next.</summary>
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

/// <summary>The Scarab level reward track.</summary>
public static class ScarabGameMilestones
{
    public static bool IsVisible => new GameElement(Paths.ScarabGameMilestonesLoc.Root).IsVisible();

    // The milestones list also holds 3 decorative siblings that aren't tiers.
    private static IEnumerable<GameElement> Tiers =>
        new GameElement(Paths.ScarabGameMilestonesLoc.MilestonesRoot).GetChildren()
            .Where(t => t.Name.StartsWith("ScarabMilestone"));

    /// <summary>Claims every ready tier, passing over the list again in case a claim readies the next.</summary>
    public static IEnumerator ClaimUntilExhausted()
    {
        const int maxPasses = 3;

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
