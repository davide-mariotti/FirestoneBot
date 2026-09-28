using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Firebot.BotActions;
using Firebot.Core.Tasks;
using Firebot.GameModel.Shared;
using Firebot.Utilities;
using MelonLoader;
using UnityEngine;
using static Firebot.Core.BotSettings;

namespace Firebot.Core;

public static class BotManager
{
    // How long an idle task (nothing to do this cycle) waits before checking again.
    private static readonly TimeSpan IdleRetryDelay = TimeSpan.FromMinutes(2);

    private static readonly System.Collections.Generic.List<BotTask> Tasks = new();

    // Where PickEligibleTask's round-robin resumes.
    private static int _lastEligibleTaskIndex = -1;

    private static object _botRoutineHandle;
    public static bool IsRunning { get; private set; }
    private static bool IsTaskExecuting { get; set; }

    public static bool ShouldPauseBackgroundTasks() => IsRunning && IsTaskExecuting;

    public static void Initialize()
    {
        const string targetNamespace = "Firebot.Tasks";
        Tasks.Clear();

        var assembly = Assembly.GetExecutingAssembly();
        var taskTypes = assembly.GetTypes()
            .Where(task => task.Namespace != null && task.Namespace.StartsWith(targetNamespace) &&
                           task.IsSubclassOf(typeof(BotTask)) && !task.IsAbstract);

        var instances = new System.Collections.Generic.List<BotTask>();
        foreach (var type in taskTypes)
            try
            {
                var task = (BotTask)Activator.CreateInstance(type);
                if (task != null) instances.Add(task);
            }
            catch (Exception e)
            {
                Logger.Info($"[Loader] Failed to load {type.Name}: {e.GetType().Name} - {e.Message}");
            }

        // Reflection returns types in no useful order. This one becomes both the .cfg section order
        // and the status table's.
        foreach (var task in instances.OrderBy(t => t.Group).ThenBy(t => t.SectionTitle))
        {
            task.InitializeConfig(ConfigPath);
            Tasks.Add(task);
        }
    }

    public static void Start()
    {
        if (IsRunning) return;
        if (Tasks.Count == 0) Initialize();

        IsRunning = true;
        _botRoutineHandle = MelonCoroutines.Start(BotSchedulerLoop());
        HeroUpgrade.Start();
        AutoRetreat.Start();
        FlyingBonusHunter.Start();
        Logger.Info($"Started. Enabled tasks: {Tasks.Count(t => t.IsEnabled)} of {Tasks.Count} loaded.");
    }

    public static void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        IsTaskExecuting = false;
        if (_botRoutineHandle != null) MelonCoroutines.Stop(_botRoutineHandle);
        HeroUpgrade.Stop();
        AutoRetreat.Stop();
        FlyingBonusHunter.Stop();
        Logger.Info("Stopped.");
    }

    private static IEnumerator BotSchedulerLoop()
    {
        if (AutoStart) yield return new WaitForSeconds(StartBotDelay);

        while (IsRunning)
        {
            // Every task's readiness check reads the level - read it once per tick.
            PlayerAvatar.RefreshCachedLevel();

            var notificationVisible = new bool[Tasks.Count];
            for (var i = 0; i < Tasks.Count; i++) notificationVisible[i] = Tasks[i].IsNotificationVisible();

            var readyTask = PickEligibleTask(notificationVisible);

            if (readyTask != null)
            {
                IsTaskExecuting = true;
                try
                {
                    yield return RunSafe(Watchdog.ForceClearAll(), $"Watchdog cleanup before {readyTask.SectionTitle}");

                    var stopwatch = Stopwatch.StartNew();

                    yield return RunSafe(readyTask.Execute(), $"Task {readyTask.SectionTitle}", readyTask.MaxRuntimeSeconds);
                    readyTask.LastRunTime = DateTime.Now;
                    readyTask.EnsureMinimumNextRun(IdleRetryDelay);
                    readyTask.PersistNextRunTime();

                    stopwatch.Stop();

                    Logger.Info(
                        $"[Task] {readyTask.SectionTitle} finished in {stopwatch.Elapsed.TotalSeconds:0.###}s | Next: {readyTask.NextRunTime:MM/dd/yyyy HH:mm:ss}");
                    PrintTasksStatusTable();

                    yield return RunSafe(Watchdog.ForceClearAll(), $"Watchdog cleanup after {readyTask.SectionTitle}");
                }
                finally
                {
                    IsTaskExecuting = false;
                }
            }

            yield return new WaitForSeconds(ScanInterval);
        }
    }

    private static IEnumerator RunSafe(IEnumerator routine, string context, float? timeoutOverride = null)
    {
        if (routine == null)
        {
            Logger.Info($"[FAILED] {context} returned null routine.");
            yield break;
        }

        var timeoutSeconds = timeoutOverride ?? MaxTaskRuntime;
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            object current = null;
            bool movedNext;

            if (stopwatch.Elapsed.TotalSeconds > timeoutSeconds)
            {
                Logger.Info($"[FAILED] {context} timed out after {timeoutSeconds:0.###}s.");
                yield break;
            }

            try
            {
                movedNext = routine.MoveNext();
                if (movedNext) current = routine.Current;
            }
            catch (Exception e)
            {
                Logger.Info($"[FAILED] {context} threw: {e.GetType().Name} - {e.Message}");
                yield break;
            }

            if (!movedNext) yield break;
            yield return current;
        }
    }

    /// <summary>In the fixed task order, so each task's row stays put between prints.</summary>
    private static void PrintTasksStatusTable()
    {
        var now = DateTime.Now;
        Logger.Info($"[Bot Status] Task Table - {now:MM/dd/yyyy HH:mm:ss}");
        Logger.Info("| Next Run            | Time Left   | Task                           | Status        | Last Run            |");
        Logger.Info("|---------------------|-------------|--------------------------------|---------------|---------------------|");

        foreach (var t in Tasks)
        {
            var status = GetTaskStatus(t);
            var nextRun = t.IsEnabled ? t.NextRunTime.ToString("MM/dd/yyyy HH:mm:ss") : "-";
            var lastRun = t.LastRunTime?.ToString("MM/dd/yyyy HH:mm:ss") ?? "-";
            var name = t.SectionTitle;
            var timeLeft = TimeParser.FormatFriendlyDuration(t.NextRunTime - now);
            Logger.Info($"| {nextRun,-19} | {timeLeft,-11} | {name,-30} | {status,-13} | {lastRun,-19} |");
        }
    }

    /// <summary>
    ///     Round-robin over every task that is eligible right now - due by time or showing its badge -
    ///     as one pool. A badge that never clears (Chaos Rift's, Path of Glory's) then only wins its
    ///     fair share of ticks instead of starving every other task.
    /// </summary>
    private static BotTask PickEligibleTask(bool[] notificationVisible)
    {
        if (Tasks.Count == 0) return null;

        for (var offset = 1; offset <= Tasks.Count; offset++)
        {
            var index = (_lastEligibleTaskIndex + offset) % Tasks.Count;
            if (!Tasks[index].IsReady(notificationVisible[index])) continue;

            _lastEligibleTaskIndex = index;
            return Tasks[index];
        }

        return null;
    }

    private static string GetTaskStatus(BotTask t)
    {
        if (!t.IsEnabled) return "Disabled";
        if (t.IsNotificationVisible()) return "Notification";
        return t.IsReady() ? "Ready" : "Waiting";
    }
}
