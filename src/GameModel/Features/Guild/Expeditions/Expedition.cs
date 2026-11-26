using System;
using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;

namespace Firebot.GameModel.Features.Guild.Expeditions;

public static class Expedition
{
    public static IEnumerator Close => new GameButton(Paths.MenusLoc.ExpeditionsLoc.CloseBtn).Click();

    public static bool IsExpeditionActive =>
        new GameElement(Paths.MenusLoc.ExpeditionsLoc.ActiveExpedition).IsVisible();

    public static DateTime NextRunTime => new GameText(Paths.MenusLoc.ExpeditionsLoc.NextRunTimeTxt).Time;

    public static IEnumerator Claim => new GameButton(Paths.MenusLoc.ExpeditionsLoc.ClaimBtn).Click();

    public static DateTime CurrentRunTime => new GameText(Paths.MenusLoc.ExpeditionsLoc.CurrentRunTimeTxt).Time;

    public static IEnumerator Start => new GameButton(Paths.MenusLoc.ExpeditionsLoc.StartBtn).Click();

    /// <summary>
    ///     Starts the pending expedition with the most Expedition Tokens per hour, then the most guild
    ///     coins (decided 26/11: the tokens hold the Personal Tree, and the War Machines behind it), by
    ///     its own start button. The first one's button, as before, when the game can't be read.
    /// </summary>
    public static IEnumerator StartBest()
    {
        string listing = null;
        var best = GameState.GetRef("expedition choice", () =>
        {
            Il2Cpp.ExpeditionInteractionPending pick = null;
            double bestRate = -1, bestCoins = -1;
            var seen = new System.Collections.Generic.List<string>();
            foreach (var pending in Il2Cpp.GameInitialize.MenuLoader.ExpeditionsPopup.expeditionPendingInteractions)
            {
                var e = pending?.expedition;
                if (e == null || pending.startButton == null || !pending.startButton.interactable ||
                    !pending.gameObject.activeInHierarchy)
                    continue;

                double tokens = e.expeditionTokenReward.GetDecrypted(), coins = e.guildCoinReward.GetDecrypted(), time = e.timeReq.GetDecrypted();
                var rate = time > 0 ? tokens / time : 0;
                seen.Add($"{e.expeditionType} {tokens:0} tokens {coins:0} coins in {time:0}");
                if (rate > bestRate || (rate == bestRate && coins > bestCoins))
                {
                    pick = pending;
                    bestRate = rate;
                    bestCoins = coins;
                }
            }

            listing = string.Join(" | ", seen);
            return pick;
        });

        if (best == null)
        {
            yield return Start;
            yield break;
        }

        Core.Logger.Debug($"[INFO] Expedition from the game: {best.expedition.expeditionType}, of {listing}.");
        best.startButton.onClick.Invoke();
        yield return Poll.AfterClick("expedition start");
    }
}
