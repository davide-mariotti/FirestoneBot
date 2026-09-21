using System;
using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Shared;

public static class Store
{
    // KNOWN BROKEN, 2026-09-20 - see DailyStoreOffersTask's own doc comment for the full live
    // diagnostic writeup: storeButton's Button component has 0 onClick listeners, and neither this
    // (ClickSimulated) nor plain Click() nor GameNotificationButton ever actually opens the screen.
    // Left as ClickSimulated (marginally more "correct" than a bare Invoke()) rather than reverted,
    // since neither one works - whoever picks this up next needs a different approach entirely.
    public static IEnumerator Open => new GameButton(Paths.BattleLoc.RightSideUILoc.StoreBtn).ClickSimulated();

    public static IEnumerator Close => new GameButton(Paths.MenusLoc.StoreLoc.CloseBtn).Click();

    public static IEnumerator OpenDailyRewardsTab =>
        new GameButton(Paths.MenusLoc.StoreLoc.TabsLoc.DailyRewardsBtn).Click();

    public static IEnumerator ClaimCheckIn =>
        new GameButton(Paths.MenusLoc.StoreLoc.DailyRewardsLoc.CheckInBtn).Click();

    public static DateTime CheckInNextRunTime =>
        new GameText(Paths.MenusLoc.StoreLoc.DailyRewardsLoc.NextRunTimeTxt).Time;

    public static IEnumerator OpenValueBundleDailyTab =>
        new GameButton(Paths.MenusLoc.StoreLoc.TabsLoc.ValueBundleDailyBtn).Click();

    /// <summary>
    ///     Claims ONLY the free mystery box slot in this tab. The numbered valueBundle (0)/(1)/(2)
    ///     slots next to it are real-money/premium-currency purchases - never wire those up here.
    /// </summary>
    public static IEnumerator ClaimFreeMysteryBox =>
        new GameButton(Paths.MenusLoc.StoreLoc.ValueBundleDailyLoc.FreeMysteryBoxBtn).Click();

    public static DateTime ValueBundleDailyRenewTime =>
        new GameText(Paths.MenusLoc.StoreLoc.ValueBundleDailyLoc.RenewTxt).Time;

    // "Pacchetti Speciali" tab - a separate tab from ValueBundleDaily above, with its own
    // independent free mystery box (see Paths.MenusLoc.StoreLoc.ExtremeValueBundlesLoc's doc
    // comment for why this is a distinct claim rather than a duplicate of ClaimFreeMysteryBox).
    public static IEnumerator OpenExtremeValueBundleTab =>
        new GameButton(Paths.MenusLoc.StoreLoc.TabsLoc.ValueBundleBtn).Click();

    /// <summary>
    ///     Claims ONLY the free mystery box card in this tab. The sibling "extremeValueBundle" card
    ///     is a real-money purchase - never wire that up here.
    /// </summary>
    public static IEnumerator ClaimFreeMysteryBoxExtreme =>
        new GameButton(Paths.MenusLoc.StoreLoc.ExtremeValueBundlesLoc.FreeMysteryBoxBtn).Click();
}
