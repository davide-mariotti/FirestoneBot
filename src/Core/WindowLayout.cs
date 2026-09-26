using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Firebot.Core;

/// <summary>
///     Optional (opt-in via window_grid_enabled) tiling of this instance's OS window into a fixed grid
///     cell, so N simultaneous instances can all be visible on one monitor without manual dragging.
///     New feature, requested by the user, 2026-09-20 - built and tested on Steam-0 only per their
///     standing instruction; NOT yet rolled out to the rest of the fleet.
///     Two independent pieces: BotSettings.ApplyLowResourceModeOnce sets the CLIENT area size (via
///     Unity's own Screen.SetResolution, same call low_resource_mode already made unconditionally -
///     now configurable instead of hardcoded 640x480 when grid mode is on) - this class only moves the
///     OS window's position afterward (SWP_NOSIZE), it never resizes. Keeping those two concerns split
///     avoids a fight between Unity's own window-sizing (driven by Screen.SetResolution, re-applied
///     every scene load) and a second, independent resize call fighting over the same window rect.
///     The grid pitch is the configured client size plus the window's VISIBLE chrome (title bar and
///     thin border, measured live via DWM - see MeasureFrame), so cells tile edge to edge without
///     overlapping regardless of Windows' title bar height or the game's window style.
/// </summary>
public static class WindowLayout
{
    private static readonly Regex InstanceIndexPattern = new(@"Steam-(\d+)", RegexOptions.Compiled);

    public static void ApplyGridPosition(int columns, int cellWidth, int cellHeight, int firstInstance)
    {
        var instance = DetectInstanceIndex();
        if (instance == null)
        {
            Logger.Debug("[WindowLayout] Could not find 'Steam-N' in the install path - skipping.");
            return;
        }

        // Cell 0 belongs to window_grid_first_instance, so a PC hosting Steam-17..34 starts at the
        // top-left instead of 3 empty rows below the bottom of the screen.
        var index = instance.Value - firstInstance;
        if (index < 0)
        {
            Logger.Debug(
                $"[WindowLayout] Instance -{instance} is below window_grid_first_instance={firstInstance} - skipping.");
            return;
        }

        var hwnd = FindGameWindow();
        if (hwnd == IntPtr.Zero)
        {
            Logger.Debug("[WindowLayout] MainWindowHandle is zero (window not ready yet?) - skipping.");
            return;
        }

        var frame = MeasureFrame(hwnd);

        // Pitch = the VISIBLE window (client + title bar + thin border), so each row starts right
        // below the previous row's frame instead of 32px over its title bar. The window is then
        // shifted by the invisible resize border so the visible edges, not the window rect, line up.
        var pitchX = cellWidth + frame.ChromeWidth;
        var pitchY = cellHeight + frame.ChromeHeight;
        var col = index % columns;
        var row = index / columns;
        var x = col * pitchX - frame.InvisibleLeft;
        var y = row * pitchY - frame.InvisibleTop;

        SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);

        Logger.Info(
            $"[WindowLayout] Instance -{instance}: grid cell ({col},{row}) of {columns} columns, pitch {pitchX}x{pitchY} -> moved to ({x},{y}).");
    }

    private readonly struct Frame
    {
        public readonly int ChromeWidth, ChromeHeight, InvisibleLeft, InvisibleTop;

        public Frame(int chromeWidth, int chromeHeight, int invisibleLeft, int invisibleTop)
        {
            ChromeWidth = chromeWidth;
            ChromeHeight = chromeHeight;
            InvisibleLeft = invisibleLeft;
            InvisibleTop = invisibleTop;
        }
    }

    /// <summary>
    ///     Title bar/border sizes don't depend on the window's size, so they're valid even if Unity
    ///     hasn't applied the new Screen.SetResolution yet. DWM's extended frame bounds are the edges
    ///     actually drawn; GetWindowRect also counts the invisible ~7px resize border on Windows 10/11.
    ///     If DWM can't be queried, falls back to the full window rect (no invisible-border offset).
    /// </summary>
    private static Frame MeasureFrame(IntPtr hwnd)
    {
        if (!GetWindowRect(hwnd, out var window) || !GetClientRect(hwnd, out var client)) return default;

        var visible = window;
        if (DwmGetWindowAttribute(hwnd, DwmwaExtendedFrameBounds, out var dwm, Marshal.SizeOf<Rect>()) == 0)
            visible = dwm;

        return new Frame(
            visible.Right - visible.Left - client.Right,
            visible.Bottom - visible.Top - client.Bottom,
            visible.Left - window.Left,
            visible.Top - window.Top);
    }

    /// <summary>
    ///     Application.dataPath is a plain Unity API (no OS process lookup needed) and always looks
    ///     like ".../Steam-N/steamapps/common/Firestone/Firestone_Data" for this multi-instance setup
    ///     - see the project's own memory notes on the Steam-0..14 layout.
    /// </summary>
    private static int? DetectInstanceIndex()
    {
        var match = InstanceIndexPattern.Match(Application.dataPath);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    ///     Process.MainWindowHandle is not reliable here: the MelonLoader console is a second top-level
    ///     window of this same process and can be picked instead, which then gets tiled while the game
    ///     window stays put. Look up the Unity window by its class name instead.
    /// </summary>
    private static IntPtr FindGameWindow()
    {
        var pid = (uint)Process.GetCurrentProcess().Id;
        var found = IntPtr.Zero;
        var className = new StringBuilder(64);
        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out var windowPid);
            if (windowPid != pid) return true;
            className.Clear();
            GetClassName(hWnd, className, className.Capacity);
            if (className.ToString() != "UnityWndClass") return true;
            found = hWnd;
            return false;
        }, IntPtr.Zero);

        // Fall back to the old lookup so a setup where the class name doesn't match keeps behaving
        // exactly as before this change.
        return found != IntPtr.Zero ? found : Process.GetCurrentProcess().MainWindowHandle;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect rect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hWnd, int attribute, out Rect value, int size);

    private const int DwmwaExtendedFrameBounds = 9;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
}
