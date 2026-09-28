using System;
using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Shared;

public static class Store
{
    // Dormant fallback: storeButton's Button has no onClick listeners and no click method opens the
    // Store through it. DailyStoreOffersTask gets in through the CheckIn/MysteryBox badges instead,
    // which are lit exactly when there's something to claim.
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

    public static IEnumerator ClaimFreeMysteryBox =>
        new GameButton(Paths.MenusLoc.StoreLoc.ValueBundleDailyLoc.FreeMysteryBoxBtn).Click();

    public static DateTime ValueBundleDailyRenewTime =>
        new GameText(Paths.MenusLoc.StoreLoc.ValueBundleDailyLoc.RenewTxt).Time;

    public static IEnumerator OpenExtremeValueBundleTab =>
        new GameButton(Paths.MenusLoc.StoreLoc.TabsLoc.ValueBundleBtn).Click();

    public static IEnumerator ClaimFreeMysteryBoxExtreme =>
        new GameButton(Paths.MenusLoc.StoreLoc.ExtremeValueBundlesLoc.FreeMysteryBoxBtn).Click();
}
