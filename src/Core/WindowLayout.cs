using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
///     The window's actual OUTER size (including title bar/borders) is used as the grid pitch (queried
///     live via GetWindowRect right after positioning, not assumed) so cells never overlap regardless
///     of whatever chrome size Windows/this game's borderless-or-not window style actually has.
/// </summary>
public static class WindowLayout
{
    private static readonly Regex InstanceIndexPattern = new(@"Steam-(\d+)", RegexOptions.Compiled);

    public static void ApplyGridPosition(int columns, int cellWidth, int cellHeight)
    {
        var index = DetectInstanceIndex();
        if (index == null)
        {
            Logger.Debug("[WindowLayout] Could not find 'Steam-N' in the install path - skipping.");
            return;
        }

        var hwnd = Process.GetCurrentProcess().MainWindowHandle;
        if (hwnd == IntPtr.Zero)
        {
            Logger.Debug("[WindowLayout] MainWindowHandle is zero (window not ready yet?) - skipping.");
            return;
        }

        var col = index.Value % columns;
        var row = index.Value / columns;
        var x = col * cellWidth;
        var y = row * cellHeight;

        SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);

        Logger.Info(
            $"[WindowLayout] Instance -{index}: grid cell ({col},{row}) of {columns} columns -> moved to ({x},{y}).");
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);

    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
}
