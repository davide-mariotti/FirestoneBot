using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Shared;

public static class SystemMail
{
    // See UiVariantButton - this HUD region has two live variants depending on the client, only
    // one populated per session.
    public static IEnumerator Open => UiVariantButton.Click(
        new GameButton(Paths.BattleLoc.LeftSideUINewLoc.MailBtn),
        new GameButton(Paths.BattleLoc.BottomLeftSideUILoc.MailBtn));

    public static GameElement MailList => new(Paths.SystemMailLoc.MailListRoot);

    public static GameButton ClaimBtn => new(Paths.SystemMailLoc.ClaimBtn);

    public static IEnumerator Close => new GameButton(Paths.SystemMailLoc.CloseBtn).Click();
}
