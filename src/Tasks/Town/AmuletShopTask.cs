using System;
using System.Collections;
using System.Linq;
using Firebot.Core;
using Firebot.Core.Tasks;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Firebot.Utilities;
using MelonLoader;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Buys the Store's amulet of the day for 2000 gems when AmuletPlanner says so (AMULETS_PLAN.md).
///     The decision is read from the game's data every hour - gems from quests can cross the line
///     mid-day - and logged every time; the Store opens at most once a game-day, only to buy. With
///     dry_run (the default) it checks the card and logs what it would buy, without clicking.
/// </summary>
public class AmuletShopTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(1);

    private MelonPreferences_Entry<bool> _dryRun;
    private MelonPreferences_Entry<int> _gemReserve;
    private MelonPreferences_Entry<string> _lastDoneDate;

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_dryRun != null) return;

        _dryRun = category.CreateEntry("dry_run", true, "Dry Run",
            "Opens the Store and checks the amulet card, but never clicks buy. Set false to spend gems. " +
            "Default: true.");

        _gemReserve = category.CreateEntry("gem_reserve", 0, "Gem Reserve",
            "Never buy an amulet that would leave fewer gems than this. Default: 0.");

        _lastDoneDate = category.CreateEntry("last_done_date", "", "Last Done Date",
            "(auto-managed, don't edit) - the last game-day the Store was opened to buy, real or dry run.");
    }

    public override IEnumerator Execute()
    {
        NextRunTime = DateTime.Now + RecheckDelay;

        if (!Amulets.TryRead(out var today, out var owned, out var gems))
        {
            Debug("[FAILED] Amulet of the day: the game's amulet data isn't readable.");
            yield break;
        }

        var decision = AmuletPlanner.Decide(today, owned, PlayerAvatar.CharacterLevel, gems, _gemReserve.Value);
        var ownedNames = string.Join(" ", owned.Select(c => c.Replace("AmuletOf", "")).OrderBy(n => n));
        Debug($"[INFO] Amulet of the day: {today} (tier {decision.Tier}), gems {gems:0}, owned {owned.Count} " +
              $"[{ownedNames}], {(decision.Buy ? "buy" : "skip")}: {decision.Reason}.");
        if (!decision.Buy || _lastDoneDate.Value == GameDay.Today()) yield break;

        // Marked before the attempt: a failed or cut-off one waits for tomorrow, it never buys twice.
        _lastDoneDate.Value = GameDay.Today();

        yield return TownScreen.Open;
        yield return TownScreen.OpenStore;
        var tab = new GameButton(Paths.MenusLoc.StoreLoc.TabsLoc.AmuletsBtn);
        yield return Poll.Until(tab.IsVisible, 20);
        yield return tab.Click();
        yield return Poll.Until(() => Amulets.ReadCard().Code != null, 20);

        var (code, price) = Amulets.ReadCard();
        var buy = new GameButton(Paths.MenusLoc.StoreLoc.AmuletsLoc.PurchaseGemsBtn);
        var clickable = buy.IsClickable();
        Debug($"[INFO] Amulet card: {code ?? "not shown"}, gem price '{price}', gem button clickable {clickable}.");

        var digits = new string((price ?? "").Where(char.IsDigit).ToArray());
        if (code != today || digits != AmuletPlanner.Price.ToString() || !clickable)
            Debug($"[FAILED] Amulet card doesn't match {today} at {AmuletPlanner.Price} gems - nothing bought.");
        else if (_dryRun.Value)
            Debug($"[INFO] Amulet of the day: dry run, would buy {today} now.");
        else
        {
            yield return buy.Click();
            // The gem button only opens popups/SpendConfirm; its confirm buys (Steam-10, 26/10).
            var confirm = new GameButton(Paths.MenusLoc.SpendConfirmLoc.ConfirmBtn);
            yield return Poll.Until(confirm.IsVisible, 10);
            if (confirm.IsVisible()) yield return confirm.Click();
            else
                Debug($"[FAILED] Amulet purchase: no confirm button. Screens {Watchdog.DumpActiveScreens()}, " +
                      $"SpendConfirm:\n{Watchdog.DumpChildrenRecursive(Paths.MenusLoc.SpendConfirmLoc.Root, 3)}");
            yield return Poll.Until(() => Amulets.Owns(today), 20);
            Amulets.TryRead(out _, out _, out var after);
            var bought = Amulets.Owns(today);
            Debug($"[{(bought ? "INFO" : "FAILED")}] Amulet bought: {today} owned {bought}, gems {gems:0} -> {after:0}.");
        }

        yield return Store.Close;
        yield return TownScreen.Close;
    }
}
