using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PeekDesktop;

/// <summary>
/// Captures the state of all visible top-level windows, minimizes them,
/// and restores them to their exact previous positions (including maximized state).
/// </summary>
public sealed class WindowTracker
{
    private readonly List<WindowInfo> _savedWindows = new();

    public bool HasWindows => _savedWindows.Count > 0;

    /// <summary>
    /// Snapshot all visible, non-system top-level windows and their placements.
    /// </summary>
    public void CaptureWindows()
    {
        _savedWindows.Clear();

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (ShouldTrackWindow(hwnd))
            {
                var placement = new NativeMethods.WINDOWPLACEMENT();
                placement.length = Marshal.SizeOf<NativeMethods.WINDOWPLACEMENT>();
                if (NativeMethods.GetWindowPlacement(hwnd, ref placement))
                {
                    _savedWindows.Add(new WindowInfo(hwnd, placement));
                }
            }
            return true;
        }, IntPtr.Zero);
    }

    /// <summary>
    /// Minimize every captured window.
    /// </summary>
    public void MinimizeAll()
    {
        foreach (var window in _savedWindows)
        {
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_MINIMIZE);
        }
    }

    /// <summary>
    /// Restore every captured window to its saved placement.
    /// Restores bottom-to-top to preserve Z-order, and does NOT steal focus.
    /// </summary>
    public void RestoreAll()
    {
        // Restore in reverse order (bottom windows first) to preserve Z-order
        for (int i = _savedWindows.Count - 1; i >= 0; i--)
        {
            var info = _savedWindows[i];

            // Skip windows that were destroyed while we were peeking
            if (!NativeMethods.IsWindow(info.Handle))
                continue;

            var placement = info.Placement;
            NativeMethods.SetWindowPlacement(info.Handle, ref placement);
        }

        _savedWindows.Clear();
    }

    /// <summary>
    /// Determines whether a window should be captured for peek/restore.
    /// Filters out system chrome, invisible windows, tool windows, etc.
    /// </summary>
    private static bool ShouldTrackWindow(IntPtr hwnd)
    {
        if (!NativeMethods.IsWindowVisible(hwnd))
            return false;

        if (NativeMethods.IsIconic(hwnd))
            return false;

        // Skip owned windows — they follow their owner
        if (NativeMethods.GetWindow(hwnd, NativeMethods.GW_OWNER) != IntPtr.Zero)
            return false;

        // Skip cloaked windows (other virtual desktops, hidden UWP apps)
        if (NativeMethods.IsWindowCloaked(hwnd))
            return false;

        string className = NativeMethods.GetWindowClassName(hwnd);
        if (string.IsNullOrEmpty(className))
            return false;

        // Skip shell and system windows
        if (IsExcludedClass(className))
            return false;

        // Skip tool windows (floating palettes, etc.)
        long exStyle = NativeMethods.GetWindowLongValue(hwnd, NativeMethods.GWL_EXSTYLE);
        if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0)
            return false;
        if ((exStyle & NativeMethods.WS_EX_NOACTIVATE) != 0)
            return false;

        return true;
    }

    private static bool IsExcludedClass(string className)
    {
        return className switch
        {
            "Progman" => true,
            "WorkerW" => true,
            "Shell_TrayWnd" => true,
            "Shell_SecondaryTrayWnd" => true,
            "NotifyIconOverflowWindow" => true,
            "DV2ControlHost" => true,            // Start menu (Win10)
            "Windows.UI.Core.CoreWindow" => true, // Start, Action Center
            _ => false
        };
    }

    private record WindowInfo(IntPtr Handle, NativeMethods.WINDOWPLACEMENT Placement);
}
