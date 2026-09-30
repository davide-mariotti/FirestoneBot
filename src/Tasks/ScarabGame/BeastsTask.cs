using System;
using System.Collections;
using Firebot.Core.Tasks;
using Firebot.GameModel.Features.ScarabGame;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using ScarabGameScreen = Firebot.GameModel.Features.ScarabGame.ScarabGame;
using TownScreen = Firebot.GameModel.Features.Town.Town;

namespace Firebot.Tasks.ScarabGame;

/// <summary>
///     Releases the beast once the Scarab's Game tablet has its six sigils, then spends Soul Embers on
///     beast levels, one level per beast per round so the cheap levels go first. Rarity (Cobra Keys) is
///     left alone.
/// </summary>
public class BeastsTask : BotTask
{
    internal override TaskGroup Group => TaskGroup.ScarabGame;
    protected override int MinimumCharacterLevel => 60;

    internal override float? MaxRuntimeSeconds => 600f;

    protected override string NotificationBadgeName => Paths.BattleLoc.NotificationsLoc.ScarabGameBeastRelease;

    private static readonly TimeSpan RecheckDelay = TimeSpan.FromHours(12);

    private const int MaxRounds = 30;

    private static readonly UnityEngine.WaitForSeconds SelectDelay = new(0.3f);

    public override IEnumerator Execute()
    {
        yield return Notifications.ScarabGameBeastRelease;

        yield return TownScreen.Open;
        yield return TownScreen.OpenScarabGame;

        if (ScarabGameScreen.ReleaseBeastBtn.IsClickable())
        {
            yield return ScarabGameScreen.ReleaseBeastBtn.Click();
            Debug("[INFO] Beasts: released a beast.");
            yield return Poll.Until(() => !ScarabGameScreen.ReleaseBeastBtn.IsClickable());
            yield return ScarabGameScreen.CloseReleasedBeast;
        }

        yield return ScarabGameScreen.OpenBeasts;

        // The list holds all 30 beasts and seems to re-sort after a level-up (30/09: a fresh beast's
        // saved index pointed elsewhere on the next round), so every round looks for the owned ones again.
        var owned = 0;
        var levels = 0;
        for (var round = 0; round < MaxRounds; round++)
        {
            var bought = false;
            owned = 0;
            for (var i = 0; i < Beasts.Count; i++)
            {
                if (!Beasts.TrySelect(i)) continue;

                yield return SelectDelay;
                if (!Beasts.IsSelectedOwned) continue;

                owned++;
                yield return Beasts.OpenUpgradePopup;

                var btn = BeastModify.ModifyBtn;
                var icon = BeastModify.CostIcon;
                if (btn.IsClickable() && icon == "soulEmber64")
                {
                    var before = BeastModify.SoulEmbers;
                    yield return btn.Click();
                    yield return Poll.Until(() => BeastModify.SoulEmbers < before);

                    var after = BeastModify.SoulEmbers;
                    if (after < before)
                    {
                        bought = true;
                        levels++;
                        Debug($"[INFO] Beasts: beast {i} levelled, Soul Embers {before} -> {after}.");
                    }
                    else
                    {
                        Debug($"[FAILED] Beasts: beast {i} upgrade clicked but Soul Embers stayed at {before}.");
                    }
                }
                else if (btn.IsVisible() && icon != "soulEmber64")
                {
                    Debug($"[INFO] Beasts: beast {i} upgrade costs '{icon}', not Soul Embers - skipped.");
                }

                yield return BeastModify.Close;
            }

            if (!bought) break;
        }

        Debug($"[INFO] Beasts: {owned} owned ('{Beasts.CountText}'), {levels} level(s) bought.");

        yield return Beasts.Close;
        yield return ScarabGameScreen.Close;
        yield return TownScreen.Close;

        NextRunTime = DateTime.Now + RecheckDelay;
    }
}
