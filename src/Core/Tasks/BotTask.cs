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
///     Sections of the config file and the status table, in this order. "Quests" groups the tasks
///     that drive the daily quests, whatever screen each one uses.
/// </summary>
public enum TaskGroup
{
    Quests,
    Town,
    Guild,
    Map,
    Warfront,
    Character,
    ScarabGame,
    Events
}

/// <summary>
///     A scheduled job. BotManager runs one ready task at a time: a task is ready once NextRunTime
///     has passed, or when one of its notification badges is visible (see BadgeCooldown). Every
///     task gets a config section with an "enabled" switch, plus whatever settings it adds in
///     OnConfigure.
/// </summary>
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

    internal abstract TaskGroup Group { get; }

    private static string GroupLabel(TaskGroup group) => group switch
    {
        TaskGroup.ScarabGame => "Scarab Game",
        _ => group.ToString()
    };

    // Set only where the class-derived name would repeat the group ("Warfront - Warfront Campaign Loot").
    protected virtual string DisplayName => null;

    public string SectionTitle
    {
        get
        {
            var group = GroupLabel(Group);
            var name = DisplayName ?? Humanize(GetType().Name);
            return name == group ? group : $"{group} - {name}";
        }
    }

    public DateTime NextRunTime { get; protected set; } = DateTime.MinValue;

    public DateTime? LastRunTime { get; set; }

    /// <summary>A badge on the notification rail (a NotificationsLoc name) that makes this task ready.</summary>
    protected virtual string NotificationBadgeName => null;

    /// <summary>Full badge paths for anything NotificationBadgeName can't express; any visible one counts.</summary>
    protected virtual string[] NotificationPaths => null;

    /// <summary>Full paths of rail badges, under both HUD roots the rail can live in.</summary>
    protected static string[] RailBadges(params string[] badgeNames) => badgeNames
        .SelectMany(name => new[]
        {
            Paths.BattleLoc.NotificationsLoc.Root + "/" + name,
            Paths.BattleLoc.NotificationsLoc.FallbackRoot + "/" + name
        })
        .ToArray();

    /// <summary>
    ///     The character level the feature unlocks at, per the wiki. Below it the task is simply
    ///     never ready, and it becomes ready within a scan of the account reaching it.
    /// </summary>
    protected virtual int MinimumCharacterLevel => 0;

    private bool MeetsLevelRequirement => PlayerAvatar.CharacterLevel >= MinimumCharacterLevel;

    /// <summary>
    ///     "enabled" for a new config file. False only for a task that shouldn't run until someone
    ///     turns it on deliberately; existing files keep whatever they already say.
    /// </summary>
    protected virtual bool DefaultEnabled => true;

    /// <summary>
    ///     Overrides BotSettings.MaxTaskRuntime for a task whose legitimate worst case is longer
    ///     (real battles, long rosters). A task cut off by the timeout is safe: Watchdog closes
    ///     whatever it left open.
    /// </summary>
    internal virtual float? MaxRuntimeSeconds => null;

    public bool IsEnabled => _enabledEntry != null && _enabledEntry.Value;

    private GameElement[] NotificationElements
    {
        get
        {
            if (_notificationElements != null) return _notificationElements;

            var paths = !string.IsNullOrEmpty(NotificationBadgeName)
                ? RailBadges(NotificationBadgeName)
                : NotificationPaths;

            if (paths != null) _notificationElements = paths.Select(p => new GameElement(p)).ToArray();

            return _notificationElements;
        }
    }

    public void InitializeConfig(string configPath)
    {
        if (_enabledEntry != null) return;

        var sectionId = _className.ToLowerInvariant();
        _category = MelonPreferences.CreateCategory(sectionId, $"{SectionTitle} Settings");
        _category.SetFilePath(configPath);

        // The separator lives in "enabled"'s own comment: MelonPreferences can't put text above a
        // section header, so this is the nearest way to mark where each section starts.
        _enabledEntry = _category.CreateEntry("enabled", DefaultEnabled, "Enable Task",
            "- - - - - - - - - - - - - - - - - - - - - - - - - -");

        OnConfigure(_category);

        _nextRunTimeEntry = _category.CreateEntry("next_run_time_internal", "", "Next Run Time (internal)",
            "(auto-managed, don't edit)");

        if (DateTime.TryParse(_nextRunTimeEntry.Value, out var savedNextRunTime) && savedNextRunTime > DateTime.Now)
            NextRunTime = savedNextRunTime;

        _category.SaveToFile();
    }

    /// <summary>Saves NextRunTime so a restart doesn't forget a real in-game cooldown.</summary>
    public void PersistNextRunTime()
    {
        if (_nextRunTimeEntry == null) return;

        _nextRunTimeEntry.Value = NextRunTime.ToString("O");
        _category.SaveToFile();
    }

    protected virtual void OnConfigure(MelonPreferences_Category category) { }

    public bool IsReady() => IsReady(IsNotificationVisible());

    // A badge that stays lit after the task's own run would otherwise make it ready on every scan:
    // Chaos Rift and Awakening ran every 40-50 s that way (106 minutes in the 26-29/09 logs). Only the
    // badge waits - NextRunTime, and with it the short retry after a failed run, is unaffected.
    private static readonly TimeSpan BadgeCooldown = TimeSpan.FromMinutes(30);

    /// <summary>
    ///     Takes the badge state the caller already computed. A visible badge already implies the
    ///     task is enabled and unlocked, so checking IsEnabled first only saves work.
    /// </summary>
    public bool IsReady(bool notificationVisible)
        => IsEnabled && MeetsLevelRequirement &&
           ((notificationVisible && (LastRunTime == null || DateTime.Now - LastRunTime >= BadgeCooldown)) ||
            DateTime.Now >= NextRunTime);

    /// <summary>
    ///     Virtual for a task whose badge can stay lit after its own work is done (MinerQuestTask):
    ///     BadgeCooldown only spaces those runs out, an override can rule them out.
    /// </summary>
    public virtual bool IsNotificationVisible()
        => IsEnabled && MeetsLevelRequirement && NotificationElements != null &&
           UiVariantButton.AnyVisible(NotificationElements);

    public abstract IEnumerator Execute();

    /// <summary>
    ///     Called after every run. When the task scheduled nothing itself (no cooldown to wait for),
    ///     retries after minDelay instead of on the very next scan. A visible badge still makes the
    ///     task ready regardless, once BadgeCooldown has passed.
    /// </summary>
    public void EnsureMinimumNextRun(TimeSpan minDelay)
    {
        var floor = DateTime.Now + minDelay;
        if (NextRunTime < floor) NextRunTime = floor;
    }

    protected void Debug(string message, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        => Logger.Debug($"[{_className}::{member}:{line}] {message}");
}
