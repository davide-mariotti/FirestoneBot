using System.Collections;
using Firebot.GameModel.Primitives;
using Firebot.Infrastructure;
using Il2Cpp;
using UnityEngine;
using Logger = Firebot.Core.Logger;

namespace Firebot.GameModel.Features.Guild;

public static class Awakening
{
    private static readonly string[] QuantityButtonsDescending =
    {
        Paths.AwakeningLoc.QuantityBtn160,
        Paths.AwakeningLoc.QuantityBtn80,
        Paths.AwakeningLoc.QuantityBtn40,
        Paths.AwakeningLoc.QuantityBtn20,
        Paths.AwakeningLoc.QuantityBtn10,
        Paths.AwakeningLoc.QuantityBtn5,
        Paths.AwakeningLoc.QuantityBtn2,
        Paths.AwakeningLoc.QuantityBtn1
    };

    // How long the crystals must stay put for the awakening, or the auto run, to count as over. Auto
    // awakened every 4-5.3 s on Steam-9 (04/10).
    private const float SettleSeconds = 10f;

    public static GameButton AwakenBtn => new(Paths.AwakeningLoc.AwakenBtn);

    private static GameButton AutoToggle => new(Paths.AwakeningLoc.AutoToggleBtn);

    // The mode the toggle shows ('Manual' / 'Auto'). It stays on after the run ends.
    private static bool AutoOn => GameInitialize.MenuLoader?.AwakeningMech?.autoAwakenOn ?? false;

    private static int Balance => (int)new GameText(Paths.AwakeningLoc.BalanceTxt).GetParsedDoubleAbbreviated(-1);

    /// <summary>
    ///     Selects the biggest multiplier the crystals pay for. A multiplier button stays clickable
    ///     when its cost isn't covered; only the awaken button reflects it (04/10: 489 crystals, x1, x2
    ///     and x5 clickable, Awaken not), so each one is tried from the top down.
    /// </summary>
    public static IEnumerator SelectBestMultiplier()
    {
        foreach (var path in QuantityButtonsDescending)
        {
            var button = new GameButton(path);
            if (!button.IsClickable()) continue;

            yield return button.Click();
            if (AwakenBtn.IsClickable()) yield break;
        }
    }

    /// <summary>
    ///     Auto instead of manual (the user's choice, 04/10): the game then awakens again by itself
    ///     while the crystals last, stepping down to x1 when they no longer cover the multiplier. The
    ///     toggle shows only while the selected multiplier is covered.
    /// </summary>
    public static IEnumerator TurnOnAuto()
    {
        if (AutoToggle.IsClickable() && !AutoOn) yield return AutoToggle.Click();
    }

    /// <summary>
    ///     Awakens, then waits until the crystals stop dropping: one awakening in manual, the whole run
    ///     in auto. Clicking on while an awakening plays cut it short (one per run until 04/10).
    /// </summary>
    public static IEnumerator Awaken()
    {
        var before = Balance;
        var cost = new GameText(Paths.AwakeningLoc.CostTxt).GetParsedText();
        var auto = AutoOn;
        yield return AwakenBtn.Click();
        yield return Poll.Until(() => Balance != before, 20);

        var wait = new WaitForSeconds(0.5f);
        var last = Balance;
        for (var still = 0f; still < SettleSeconds; still += 0.5f)
        {
            yield return wait;
            if (Balance == last) continue;

            last = Balance;
            still = 0f;
        }

        Logger.Debug($"[INFO] Awakening for {cost}{(auto ? " (auto)" : "")}: crystals {before} -> {Balance}.");
    }

    public static IEnumerator Close => new GameButton(Paths.AwakeningLoc.CloseBtn).Click();
}
