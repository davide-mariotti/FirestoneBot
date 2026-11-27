using System;
using System.Collections;
using System.Linq;
using Firebot.Core.Tasks;
using Firebot.GameModel.Base;
using Firebot.GameModel.Features.Town.MagicQuarters;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using CurrencyMissingPopup = Firebot.GameModel.Shared.CurrencyMissingPopup;
using Logger = Firebot.Core.Logger;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.Town;

/// <summary>
///     Keeps a guardian training in Magic Quarters, the one GuardianPlanner picks: every guardian to
///     evolution 5 (Vermilion, Grace, Ankaa, Azhar), then every one to 10; past that guardian_index.
///     Strange Dust above strange_dust_reserve goes into enlightening the same guardian.
/// </summary>
public class GuardianTrainingTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.Town;

    private MelonPreferences_Entry<int> _guardianIndex;
    private MelonPreferences_Entry<int> _strangeDustReserve;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.GuardianTraining;

    public override IEnumerator Execute()
    {
        yield return Notifications.GuardianTraining;

        yield return TownScreen.Open;
        yield return TownScreen.OpenMagicQuarters;

        var (target, square, evolutionDue) = Target(GetGuardianIndex());
        if (target != null)
        {
            yield return new GameButton(transform: square).Click();
            yield return MagicQuarters.TrainBtn.Click();

            // The free training goes on to the next guardian, the dust waits for the one evolving first.
            var reserve = evolutionDue ? int.MaxValue : _strangeDustReserve?.Value ?? 10000;
            var dust = StrangeDust;
            var done = 0;
            yield return EnlightenSelected(_ => StrangeDust - EnlightenmentCost >= reserve, d => done = d, true);
            if (done > 0)
                Debug($"[INFO] Enlightened {target.Name} {done} times: Strange Dust {dust:0} -> {StrangeDust:0} (reserve {reserve}).");
        }

        NextRunTime = MagicQuarters.NextRunTime;

        yield return MagicQuarters.CloseLockedPopup;
        yield return MagicQuarters.Close;
        yield return TownScreen.Close;
    }

    protected override void OnConfigure(MelonPreferences_Category category)
    {
        if (_guardianIndex != null) return;

        _guardianIndex = category.CreateEntry(
            "guardian_index",
            0,
            "Guardian Index",
            "The guardian trained once every unlocked one is past evolution 10. Use 0-3. Default is 0.\n" +
            "0=Vermilion, 1=Grace, 2=Ankaa, 3=Azhar. Before that, each unlocked guardian goes to evolution 5 in this order, then each to 10."
        );

        _strangeDustReserve = category.CreateEntry(
            "strange_dust_reserve",
            10000,
            "Strange Dust Reserve",
            "Strange Dust above this goes into enlightening the guardian in training. Default is 10000."
        );
    }

    /// <summary>
    ///     In the open Magic Quarters: GuardianPlanner's pick, else guardian_index's guardian if unlocked,
    ///     else Vermilion, with its square, and whether a guardian waits for its evolution. By name: the
    ///     squares' order isn't fixed (Steam-17, 26/11: Grace in "guardian (0)").
    /// </summary>
    private static (GuardianState Target, Transform Square, bool EvolutionDue) Target(int fallback)
    {
        var squares = (GameElement.FindTransform(Paths.MenusLoc.MagicQuartersLoc.GuardiansRoot)?
                .GetComponentsInChildren<GuardianInteractionSquare>(true) ?? new GuardianInteractionSquare[0])
            .Where(s => s.guardian != null)
            // evolutionStage counts the evolutions done (0 on a never-trained Grace, Steam-17 26/11): +1 is the wiki's number.
            .ToDictionary(s => new GuardianState(s.guardian.guardianName, s.guardian.IsUnlocked, s.guardian.evolutionStage + 1,
                s.guardian.level, s.guardian.readyToEvole), s => s.transform);

        var guardians = squares.Keys.ToList();
        var target = GuardianPlanner.Pick(guardians)
                     ?? guardians.FirstOrDefault(g => g.Unlocked && g.Name == GuardianPlanner.Order[fallback])
                     ?? guardians.FirstOrDefault(g => g.Name == "Vermilion");
        Logger.Debug($"[INFO] Guardians: {string.Join(", ", guardians.Select(Describe))}; training {target?.Name ?? "none"}.");
        return (target, target == null ? null : squares[target], guardians.Any(g => g.Unlocked && g.ReadyToEvolve));
    }

    private static string Describe(GuardianState g) =>
        !g.Unlocked ? $"{g.Name} locked" : $"{g.Name} evolution {g.Evolution} level {g.Level}{(g.ReadyToEvolve ? " ready to evolve" : "")}";

    private static double StrangeDust => GameInitialize.HandlerLoader.CurrencyHandler.GetCurrency(CurrencyCode.StrangeDust).Amount;

    // "20" a click (Steam-0, 30/09); infinite when unreadable, which no reserve check passes.
    private static double EnlightenmentCost =>
        new GameText(Paths.MenusLoc.MagicQuartersLoc.EnlightenmentCostTxt).GetParsedDoubleAbbreviated(double.PositiveInfinity);

    /// <summary>
    ///     An event challenge's enlightenments (EventChallengeActions): up to `times` on the guardian in
    ///     training. Every click counts, confirmed or not, so a missed one can never turn into an extra:
    ///     the challenge's next read tells.
    /// </summary>
    public static IEnumerator Enlighten(int times, Action<int> onDone)
    {
        yield return TownScreen.Open;
        yield return TownScreen.OpenMagicQuarters;
        yield return new GameButton(transform: Target(0).Square).Click();

        var done = 0;
        yield return EnlightenSelected(d => d < times, d => done = d);
        if (done < times)
            Logger.Debug($"[INFO] Enlightenment: {done}/{times} - the button isn't clickable (not enough Strange Dust?).");
        onDone(done);

        yield return MagicQuarters.Close;
        yield return TownScreen.Close;
    }

    /// <summary>
    ///     Enlightens the selected guardian while more(clicks so far) holds, only while the price is
    ///     Strange Dust. Stops at the first click the dust doesn't confirm, or at level 10, where the
    ///     button turns off until the evolution (Grace after 41 clicks, Steam-17 26/11).
    /// </summary>
    private static IEnumerator EnlightenSelected(Func<int, bool> more, Action<int> onDone, bool bulk = false)
    {
        // In bulk the game's own quantity button goes x20, then x10, x5 and x1 as the dust runs short
        // or a bigger step is refused: 78 single clicks took 56 s on Steam-27 (27/11). EnlightenmentCost
        // is the shown price, the quantity's, so more() keeps the reserve. An event's exact count stays
        // at x1, and the button is left at x1.
        var done = 0;
        foreach (var quantity in bulk ? new[] { 20, 10, 5, 1 } : new[] { 1 })
        {
            var set = false;
            yield return SetQuantity(quantity, ok => set = ok);
            if (!set && quantity != 1) continue;
            var step = set ? quantity : 1;

            yield return EnlightenAt(step, more, () => done, d => done = d);
        }

        yield return SetQuantity(1, _ => { });
        onDone(done);
    }

    private static Il2Cpp.MagicQuartersInfoSubmenu Info => GameInitialize.MenuLoader.MagicQuartersInfoSubmenu;

    private static int? Quantity => GameState.Get("enlighten quantity", () => Info.currentEnlightQuantity);

    /// <summary>Presses the game's quantity button until it reads `wanted`; false when it can't.</summary>
    private static IEnumerator SetQuantity(int wanted, Action<bool> done)
    {
        for (var i = 0; i < 5 && Quantity is { } current && current != wanted; i++)
        {
            if (GameState.Get("enlighten quantity button", () =>
                {
                    Info.changeQuantityButton.onClick.Invoke();
                    return true;
                }) != true)
                break;
            yield return Poll.AfterClick("enlighten quantity");
        }

        done(Quantity == wanted);
    }

    private static IEnumerator EnlightenAt(int step, Func<int, bool> more, Func<int> getDone, Action<int> setDone)
    {
        var done = getDone();
        var button = MagicQuarters.EnlightenmentBtn;
        while (more(done) && button.IsClickable())
        {
            var icon = IconSprite.NameAt(Paths.MenusLoc.MagicQuartersLoc.EnlightenmentCostIcon);
            if (icon != "strangeDust64")
            {
                Logger.Debug($"[INFO] Enlightenment costs '{EnlightenmentCost}' in '{icon}', not Strange Dust - skipped.");
                break;
            }

            var before = StrangeDust;
            yield return button.Click();
            if (CurrencyMissingPopup.IsShowing)
            {
                yield return CurrencyMissingPopup.Close;
                break;
            }

            // Counted before it's confirmed, as always: a missed click never turns into an extra one.
            done += step;
            setDone(done);
            yield return Poll.Until(() => StrangeDust < before);
            if (StrangeDust >= before) break;
        }
    }

    private int GetGuardianIndex()
    {
        var value = _guardianIndex?.Value ?? 0;

        if (value >= 0 && value < GuardianPlanner.Order.Length) return value;

        Debug($"[FAILED] Invalid guardian_index '{value}'. Using default '0'.");
        return 0;
    }
}
