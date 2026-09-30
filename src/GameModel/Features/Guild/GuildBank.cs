using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild;

/// <summary>The Guild's bank (Guild -> bank). Only the donation is used, for an event challenge.</summary>
public static class GuildBank
{
    public static bool IsVisible => new GameElement(Paths.MenusLoc.GuildBankLoc.Root).IsVisible();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.GuildBankLoc.CloseBtn).Click();

    /// <summary>The player's own guild coins ("You have").</summary>
    public static int CoinCount =>
        (int)new GameText(Paths.MenusLoc.GuildBankLoc.YourCoinsTxt).GetParsedDoubleAbbreviated();

    public static string CoinIcon => IconSprite.NameAt(Paths.MenusLoc.GuildBankLoc.YourCoinsIcon);

    public static GameButton Donate1kBtn => new(Paths.MenusLoc.GuildBankLoc.Donate1kBtn);

    /// <summary>"Max": every coin the player has.</summary>
    public static GameButton DonateAllBtn => new(Paths.MenusLoc.GuildBankLoc.DonateAllBtn);
}
