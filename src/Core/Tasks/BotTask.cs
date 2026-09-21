using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using Firebot.GameModel.Base;
using Firebot.GameModel.Primitives;
using Firebot.GameModel.Shared;
using Firebot.Infrastructure;
using MelonLoader;
using static Firebot.Utilities.StringUtils;

namespace Firebot.Core.Tasks;

/// <summary>
///     Thematic grouping for the config file and the terminal status table - not a game-code
///     concept, purely so ~25 tasks stay navigable instead of appearing in whatever arbitrary order
///     reflection happens to return them in. Declaration order here IS display order (both places
///     sort by this first). "Quests" groups the 6 tasks that drive/claim the 9 daily quests together
///     regardless of which in-game screen they each actually use, since that's what's relevant when
///     scanning for them - not the underlying screen a Town/Guild/Map/etc. grouping would imply.
///     "Map" and "Warfront" are both reached from the same WorldMap screen (its two tabs) but kept
///     separate per the user: Map Missions is unlocked from the start and dispatches missions in
///     ascending/descending time order (matches the original mission-dispatch behavior), while Warfront Campaign is an
///     unrelated level-50 sub-feature (war machines) with its own loot/daily-mission tasks - grouping
///     them together would hide that they're functionally unrelated beyond sharing a screen.
/// </summary>
public enum TaskGroup
{
    Quests,
    Town,
    Guild,
    Map,
    Warfront,
    Character,
    ScarabGame
}

public abstract class BotTask
{
    private readonly string _className;
    private MelonPreferences_Category _category;
    private MelonPreferences_Entry<bool> _enabledEntry;
    private MelonPreferences_Entry<string> _nextRunTimeEntry;
    private GameElement[] _notificationElements;

    protected BotTask()
    {
        _className = GetType().Name;
    }

    /// <summary>Which section of the terminal table / config file this task belongs in - see
    /// TaskGroup. Every task must declare one; there's no sensible generic default. Internal (not
    /// protected) so BotManager can sort on it directly.</summary>
    internal abstract TaskGroup Group { get; }

    private static string GroupLabel(TaskGroup group) => group switch
    {
        TaskGroup.ScarabGame => "Scarab Game",
        _ => group.ToString()
    };

    // Override only when the class-name-derived default repeats/echoes the group name awkwardly
    // (e.g. "Warfront - Warfront Campaign Loot", "Scarab Game - Scarab Game Free Token") - per the
    // user, 2026-09-20, purely cosmetic (terminal table alignment), null (default) uses the
    // humanized class name as before.
    protected virtual string DisplayName => null;

    public string SectionTitle
    {
        get
        {
            var group = GroupLabel(Group);
            var name = DisplayName ?? Humanize(GetType().Name);
            // Avoids "Quests - Quests" for a task whose humanized name already matches its group
            // (e.g. QuestsTask, which just claims quest rewards rather than driving one specific
            // quest's progress).
            return name == group ? group : $"{group} - {name}";
        }
    }

    public DateTime NextRunTime { get; protected set; } = DateTime.MinValue;

    public DateTime? LastRunTime { get; set; }

    // A full, already-resolved absolute path - for a task with its own self-contained badge
    // elsewhere (e.g. WarfrontDailyMissionsTask), not on the shared rail below, and not itself
    // split across multiple HUD variants.
    protected virtual string NotificationPath => null;

    // A bare badge name (e.g. "GuardianTraining") on the shared notification rail - see
    // NotificationElements below for why this is a name and not a full path: that rail lives
    // under one of two alternate HUD roots depending on the client, so both candidates need
    // building from the name.
    protected virtual string NotificationBadgeName => null;

    // Multiple full, already-resolved absolute paths for a task whose own self-contained badge
    // (not on the shared NotificationsLoc rail) is itself split across alternate HUD variants -
    // e.g. PathOfGloryTask's badge lives on BottomSideUIMobileLoc or BottomSideUIDesktopLoc
    // depending on the client. Any one being visible counts. The three Notification* properties
    // are mutually exclusive in practice (a task uses whichever fits its badge).
    protected virtual string[] NotificationPathCandidates => null;

    /// <summary>
    ///     Character level this task's underlying feature unlocks at, per the wiki - 0 (default)
    ///     means no known/relevant gate. Enforced generically here (IsReady/IsNotificationVisible)
    ///     instead of each task re-implementing its own "below level, reschedule later" boilerplate:
    ///     a task below its level requirement is simply never ready, and reacts within one scan cycle
    ///     of actually reaching it (no separate recheck-delay bookkeeping needed).
    /// </summary>
    protected virtual int MinimumCharacterLevel => 0;

    private bool MeetsLevelRequirement => PlayerAvatar.CharacterLevel >= MinimumCharacterLevel;

    /// <summary>
    ///     Per-task override for how long BotManager lets a single Execute() run before forcibly
    ///     abandoning it (see BotManager.RunSafe) - null (default, almost every task) means "use the
    ///     global BotSettings.MaxTaskRuntime". Exists for the rare task whose OWN legitimate worst
    ///     case (not a bug - a deliberately bounded retry loop) can run long: raising the global
    ///     limit for every task just to accommodate one would weaken the safety net everywhere else.
    ///     A task that gets cut off mid-run isn't corrupted by it - Watchdog's cleanup sweep (runs
    ///     right after, unconditionally) closes whatever got left open, same as any other interruption.
    /// </summary>
    internal virtual float? MaxRuntimeSeconds => null;

    public bool IsEnabled => _enabledEntry != null && _enabledEntry.Value;

    // See UiVariantButton - the shared rail (NotificationBadgeName) lives under one of two
    // alternate HUD roots depending on the client, only one populated per session, so both
    // candidates are checked; same idea for NotificationPathCandidates' own multiple absolute
    // paths; a plain NotificationPath is checked as-is (single candidate).
    private GameElement[] NotificationElements
    {
        get
        {
            if (_notificationElements != null) return _notificationElements;

            if (!string.IsNullOrEmpty(NotificationBadgeName))
                _notificationElements = new GameElement[]
                {
                    new(Paths.BattleLoc.NotificationsLoc.Root + "/" + NotificationBadgeName),
                    new(Paths.BattleLoc.NotificationsLoc.FallbackRoot + "/" + NotificationBadgeName)
                };
            else if (NotificationPathCandidates != null)
                _notificationElements = NotificationPathCandidates.Select(p => new GameElement(p)).ToArray();
            else if (!string.IsNullOrEmpty(NotificationPath))
                _notificationElements = new GameElement[] { new(NotificationPath) };

            return _notificationElements;
        }
    }

    public void InitializeConfig(string configPath)
    {
        if (_enabledEntry != null) return;

        var sectionId = _className.ToLowerInvariant();
        _category = MelonPreferences.CreateCategory(sectionId, $"{SectionTitle} Settings");
        _category.SetFilePath(configPath);

        // The separator is folded into "enabled"'s own comment (rather than a category-level one)
        // since MelonPreferences always renders a comment directly above its own entry, with no way
        // to place free text above the "[section]" header itself - this is the closest visual
        // equivalent, and it puts every section's toggle at a glance right under a clear break.
        _enabledEntry = _category.CreateEntry("enabled", false, "Enable Task",
            "- - - - - - - - - - - - - - - - - - - - - - - - - -");

        OnConfigure(_category);

        // Created last (not right after "enabled") so the settings OnConfigure actually cares about
        // aren't buried between two housekeeping fields.
        _nextRunTimeEntry = _category.CreateEntry("next_run_time_internal", "", "Next Run Time (internal)",
            "(auto-managed, don't edit)");

        if (DateTime.TryParse(_nextRunTimeEntry.Value, out var savedNextRunTime) && savedNextRunTime > DateTime.Now)
            NextRunTime = savedNextRunTime;

        _category.SaveToFile();
    }

    /// <summary>
    ///     Writes the current NextRunTime to disk so a bot/game restart doesn't forget a real
    ///     in-game cooldown and re-check everything immediately.
    /// </summary>
    public void PersistNextRunTime()
    {
        if (_nextRunTimeEntry == null) return;

        _nextRunTimeEntry.Value = NextRunTime.ToString("O");
        _category.SaveToFile();
    }

    protected virtual void OnConfigure(MelonPreferences_Category category) { }

    public bool IsReady() => IsReady(IsNotificationVisible());

    /// <summary>
    ///     Same result as IsReady(), but takes an already-computed notification-visible flag instead
    ///     of recomputing it - BotManager's scan loop already needs that value for its own
    ///     notification-priority check, and recomputing it here used to mean MeetsLevelRequirement
    ///     (an uncached read, see PlayerAvatar) got evaluated up to 3x per task per tick.
    ///     IsEnabled is checked first (a plain bool field) so a disabled task never touches the
    ///     level check at all - order changes performance only, not the result: notificationVisible
    ///     being true already implies IsEnabled/MeetsLevelRequirement were true (see
    ///     IsNotificationVisible's own definition).
    /// </summary>
    public bool IsReady(bool notificationVisible)
        => IsEnabled && MeetsLevelRequirement && (notificationVisible || DateTime.Now >= NextRunTime);

    public bool IsNotificationVisible()
        => IsEnabled && MeetsLevelRequirement && NotificationElements != null &&
           UiVariantButton.AnyVisible(NotificationElements);

    public abstract IEnumerator Execute();

    /// <summary>
    ///     Called after every execution. If the task's own logic already scheduled a real future run
    ///     (something was claimed, started, or is on a genuine cooldown), this does nothing. Otherwise
    ///     (nothing to do this cycle) it retries after minDelay instead of immediately re-tying for the
    ///     next scan cycle. Notification-driven tasks are unaffected: a visible notification badge is
    ///     still checked every cycle regardless of this floor.
    /// </summary>
    public void EnsureMinimumNextRun(TimeSpan minDelay)
    {
        var floor = DateTime.Now + minDelay;
        if (NextRunTime < floor) NextRunTime = floor;
    }

    protected void Debug(string message, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        => Logger.Debug($"[{_className}::{member}:{line}] {message}");
}
