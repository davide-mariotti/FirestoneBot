using System;
using System.Collections;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using Il2Cpp;
using UnityEngine;
using UnityEngine.EventSystems;
using static Firebot.Core.BotSettings;

namespace Firebot.GameModel.Features.Map.Missions;

public class MissionPin : GameElement
{
    public MissionPin(string path = null, GameElement parent = null, Transform transform = null)
        : base(path, parent, transform) { }

    public DateTime TimeRequired => new GameText(Paths.MissionPinLoc.TimeReq, this).Time;

    /// <summary>
    ///     How long the mission takes, from the game's mission behind the pin (realTimeReq); the pin's
    ///     text when that can't be read. Checked against the text (CompareDuration).
    /// </summary>
    public TimeSpan Duration => DurationFromGame ?? TimeRequired - DateTime.Now;

    private TimeSpan? DurationFromGame => GameState.Get("map mission duration", () =>
        TryGetComponent(out MapMissionInteraction pin) && pin.mapMission != null
            ? TimeSpan.FromSeconds(pin.mapMission.realTimeReq)
            : throw new InvalidOperationException("no mission behind the pin"));

    // "free", "active" or "done", from the game's mission behind the pin (missionState and its end);
    // null when unreadable. It matched the icons on every pin of -27 at 21:06 and -28 at 21:33 (27/11).
    private string StateFromGame => GameState.GetRef("map mission state", () =>
    {
        if (!TryGetComponent(out MapMissionInteraction pin) || pin.mapMission == null) throw new InvalidOperationException("no mission behind the pin");
        var m = pin.mapMission;
        if (m.missionState != MissionsHandler.MissionState.Active) return "free";
        return GameState.Ends(m.timeStarted, m.realTimeReq) is { } end && end <= DateTime.Now ? "done" : "active";
    });

    private string StateOnScreen => TickShown ? "done" : ActiveIconShown ? "active" : "free";

    public void CompareState() => GameState.Compare($"map mission state {Name}", StateFromGame, StateOnScreen);

    public void CompareDuration() =>
        GameState.CompareTimes("map mission duration", DurationFromGame is { } d ? DateTime.Now + d : null, TimeRequired);

    /// <summary>In progress or done (a done one keeps its active icon until claimed); the icons when the game can't say.</summary>
    public bool IsActive => (StateFromGame ?? StateOnScreen) != "free";

    public bool IsCompleted => (StateFromGame ?? StateOnScreen) == "done";

    private bool ActiveIconShown => new GameElement(Paths.MissionPinLoc.ActiveIcon, this).IsVisible();

    private bool TickShown => new GameElement(Paths.MissionPinLoc.Tick, this).IsVisible();

    /// <summary>The squads this mission takes (the game's squadsReq, set by its category); 1 when unreadable.</summary>
    public int SquadsRequired
    {
        get
        {
            if (!TryGetComponent(out MapMissionInteraction pin)) return 1;
            var missions = GameInitialize.HandlerLoader?.MissionsHandler?.missionsOnMap;
            for (var i = 0; missions != null && i < missions.Count; i++)
                if (missions[i].missionType == pin.missionType) return missions[i].squadsReq;
            return 1;
        }
    }

    public IEnumerator Select()
    {
        if (!IsVisible())
        {
            Debug($"[FAILED] OnClick ignored: MissionPin not visible. Path: {Path}");
            yield return new WaitForSeconds(InteractionDelay);
            yield break;
        }

        if (TryGetComponent(out MapMissionInteraction interaction))
            try
            {
                var fakeEvent = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left
                };
                interaction.OnPointerClick(fakeEvent);
            }
            catch (Exception e)
            {
                Debug($"[ERROR] Exception during Custom Click: {e.Message}. Path: {Path}");
            }
        else
            Debug($"[FAILED] MissionPin found but MapMissionInteraction component is missing. Path: {Path}");

        // The preview shows 0.1 s after the click, 0.9 s the first time it loads (Steam-28 27/11): the game's
        // own state instead of a fixed second each. Anything else (a reward, nothing) waits the second as before.
        var start = Time.realtimeSinceStartup;
        var type = interaction?.missionType;
        bool Shown() => GameState.IsMenuOpen(MenuName.PreviewMission) == true &&
                        GameState.Get("mission preview", () => GameInitialize.MenuLoader.PreviewMissionPopup.mapMission.missionType == type) == true;
        yield return Poll.Until(() => Shown() && GameState.Busy == false, (int)(InteractionDelay / 0.05f), 0.05f);
        if (Shown()) yield return new WaitForSeconds(0.2f);
        Debug($"[INFO] Mission pin: waited {Time.realtimeSinceStartup - start:0.00} s, menus {GameState.MenuStack}.");
    }
}
