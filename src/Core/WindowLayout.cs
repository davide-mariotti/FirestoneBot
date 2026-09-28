using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Firebot.Core;

/// <summary>
///     Optional (window_grid_enabled): moves this instance's window into a fixed grid cell so many
///     instances fit on one monitor. The window size is set by BotSettings through Screen.SetResolution;
///     this class only moves the window (SWP_NOSIZE) - two independent resizes would fight over it.
///     The grid pitch is the client size plus the window's visible frame, measured through DWM, so the
///     cells tile edge to edge whatever the title bar height.
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

        // Cell 0 belongs to window_grid_first_instance, so a PC hosting Steam-17..34 starts top-left.
        var index = instance.Value - firstInstance;
        if (index < 0)
        {
            Logger.Debug(
                $"[WindowLayout] Steam-{instance} is below window_grid_first_instance={firstInstance} - skipping.");
            return;
        }

        var hwnd = FindGameWindow();
        if (hwnd == IntPtr.Zero)
        {
            Logger.Debug("[WindowLayout] MainWindowHandle is zero (window not ready yet?) - skipping.");
            return;
        }

        var frame = MeasureFrame(hwnd);

        // The window is shifted by its invisible resize border so the visible edges line up.
        var pitchX = cellWidth + frame.ChromeWidth;
        var pitchY = cellHeight + frame.ChromeHeight;
        var col = index % columns;
        var row = index / columns;
        var x = col * pitchX - frame.InvisibleLeft;
        var y = row * pitchY - frame.InvisibleTop;

        SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);

        Logger.Info(
            $"[WindowLayout] Steam-{instance}: grid cell ({col},{row}) of {columns} columns, pitch {pitchX}x{pitchY} -> moved to ({x},{y}).");
    }

    private readonly record struct Frame(int ChromeWidth, int ChromeHeight, int InvisibleLeft, int InvisibleTop);

    /// <summary>
    ///     Title bar and border sizes don't depend on the window size, so this is valid before Unity
    ///     applies the new resolution. GetWindowRect also counts the invisible ~7px resize border of
    ///     Windows 10/11; DWM's extended frame bounds are the edges actually drawn. Without DWM it falls
    ///     back to the full window rect.
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

    // Every instance is installed as ".../Steam-N/steamapps/common/Firestone/".
    private static int? DetectInstanceIndex()
    {
        var match = InstanceIndexPattern.Match(Application.dataPath);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    ///     Looks the Unity window up by class name: Process.MainWindowHandle can return the MelonLoader
    ///     console instead, which is another top-level window of the same process.
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

        // A setup where the class name doesn't match still gets the plain lookup.
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
