using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Events;

/// <summary>Decorated Heroes' shop. Only Challenges and Exchange are used - never Medals, Skins or Market.</summary>
public static class DecoratedHeroesShop
{
    public static bool IsVisible => new GameElement(Paths.DecoratedHeroesShopLoc.Root).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.DecoratedHeroesShopLoc.CloseBtn).Click();

    public static IEnumerator OpenChallengesTab =>
        new GameButton(Paths.DecoratedHeroesShopLoc.ChallengesTabBtn).Click();

    public static IEnumerator OpenExchangeTab =>
        new GameButton(Paths.DecoratedHeroesShopLoc.ExchangeTabBtn).Click();

    public static readonly ExchangeTab Exchange = new("DecoratedHeroesShop",
        Paths.DecoratedHeroesShopLoc.ExchangeQuantityBtn, Paths.DecoratedHeroesShopLoc.ExchangeQuantityTxt,
        Paths.DecoratedHeroesShopLoc.ExchangeItemsRoot, Paths.DecoratedHeroesShopLoc.ExchangeItemNameTxt,
        Paths.DecoratedHeroesShopLoc.ExchangeItemBuyBtn);

    /// <summary>
    ///     Every shown card's text and progress ("5/10", "Completed"). A card above the account's level
    ///     keeps its title hidden (the alchemy one below 120, seen on Steam-1..16 on 30/09): skipped
    ///     with a silent check, since reading it would log a failure every session.
    /// </summary>
    public static List<(string Text, string Progress)> Challenges() =>
        new GameElement(Paths.DecoratedHeroesShopLoc.ChallengeGridRoot).GetChildren()
            .Where(card => GameElement.FindTransform(card.FullPath + Paths.DecoratedHeroesShopLoc.ChallengeTitleTxt)
                ?.gameObject.activeInHierarchy == true)
            .Select(card => (new GameText(Paths.DecoratedHeroesShopLoc.ChallengeTitleTxt, card).GetParsedText(),
                new GameText(Paths.DecoratedHeroesShopLoc.ChallengeProgressTxt, card).GetParsedText()))
            .ToList();

    // Each challenge card pays out up to 3 reward tiers.
    private const int MaxClaimsPerChallenge = 3;

    public static IEnumerator ClaimAllChallenges()
    {
        var cards = new GameElement(Paths.DecoratedHeroesShopLoc.ChallengeGridRoot).GetChildren().ToList();
        Logger.Debug($"[DecoratedHeroesShop] ClaimAllChallenges: {cards.Count} card(s) under ChallengeGridRoot.");

        var claimed = 0;
        foreach (var card in cards)
        {
            var claimBtn = new GameButton(Paths.DecoratedHeroesShopLoc.ChallengeClaimBtn, card);
            for (var i = 0; i < MaxClaimsPerChallenge && claimBtn.IsClickable(); i++)
            {
                yield return claimBtn.Click();
                claimed++;
            }
        }

        Logger.Debug($"[DecoratedHeroesShop] ClaimAllChallenges: claimed {claimed} time(s) across {cards.Count} card(s).");
    }
}
