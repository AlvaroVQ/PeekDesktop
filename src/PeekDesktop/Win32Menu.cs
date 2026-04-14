using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PeekDesktop;

/// <summary>
/// Win32 popup menu wrapper, replacing WinForms ContextMenuStrip.
/// </summary>
internal sealed class Win32Menu : IDisposable
{
    private IntPtr _hMenu;
    private readonly List<(uint id, Action action)> _items = new();
    private bool _disposed;

    public Win32Menu()
    {
        _hMenu = CreatePopupMenu();
    }

    public void AddItem(uint id, string text, Action onClick, bool isChecked = false)
    {
        uint flags = MF_STRING;
        if (isChecked)
            flags |= MF_CHECKED;
        AppendMenuW(_hMenu, flags, (nuint)id, text);
        _items.Add((id, onClick));
    }

    public void AddSeparator()
    {
        AppendMenuW(_hMenu, MF_SEPARATOR, 0, null);
    }

    public void SetChecked(uint id, bool isChecked)
    {
        CheckMenuItem(_hMenu, id, MF_BYCOMMAND | (isChecked ? MF_CHECKED : MF_UNCHECKED));
    }

    /// <summary>
    /// Shows the context menu at the current cursor position and executes
    /// the selected item's action.
    /// </summary>
    public void Show(IntPtr hwnd)
    {
        GetCursorPos(out NativeMethods.POINT pt);

        // Required for tray menus: the window must be foreground for the
        // menu to dismiss when the user clicks elsewhere.
        NativeMethods.SetForegroundWindow(hwnd);

        uint cmd = TrackPopupMenuEx(
            _hMenu, TPM_RETURNCMD | TPM_NONOTIFY,
            pt.x, pt.y, hwnd, IntPtr.Zero);

        // Required: send a benign message so the menu dismisses properly
        // when the user clicks outside it.
        PostMessageW(hwnd, 0 /*WM_NULL*/, IntPtr.Zero, IntPtr.Zero);

        if (cmd != 0)
        {
            foreach (var (id, action) in _items)
            {
                if (id == cmd)
                {
                    action();
                    break;
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hMenu != IntPtr.Zero)
        {
            DestroyMenu(_hMenu);
            _hMenu = IntPtr.Zero;
        }
    }

    // --- Constants ---
    private const uint MF_STRING = 0x0000;
    private const uint MF_SEPARATOR = 0x0800;
    private const uint MF_CHECKED = 0x0008;
    private const uint MF_UNCHECKED = 0x0000;
    private const uint MF_BYCOMMAND = 0x0000;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_NONOTIFY = 0x0080;

    // --- P/Invoke ---
    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, nuint uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    private static extern uint CheckMenuItem(IntPtr hMenu, uint uIDCheckItem, uint uCheck);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativeMethods.POINT lpPoint);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
