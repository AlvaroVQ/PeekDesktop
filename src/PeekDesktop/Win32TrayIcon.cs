using System;
using System.Runtime.InteropServices;

namespace PeekDesktop;

/// <summary>
/// Win32 Shell_NotifyIcon wrapper, replacing WinForms NotifyIcon.
/// </summary>
internal sealed class Win32TrayIcon : IDisposable
{
    public const uint WM_TRAYICON = 0x0400 + 1; // WM_USER + 1

    private readonly IntPtr _hwnd;
    private IntPtr _hIcon;
    private bool _added;
    private bool _disposed;

    public Win32TrayIcon(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    /// <summary>
    /// Adds the tray icon with the given tooltip and icon handle.
    /// </summary>
    public void Add(IntPtr hIcon, string tooltip)
    {
        _hIcon = hIcon;
        var nid = MakeNid();
        nid.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        nid.uCallbackMessage = WM_TRAYICON;
        nid.hIcon = hIcon;
        SetTip(ref nid, tooltip);

        Shell_NotifyIconW(NIM_ADD, ref nid);

        // Request version 4 behavior for richer messages
        nid.uVersion = NOTIFYICON_VERSION_4;
        Shell_NotifyIconW(NIM_SETVERSION, ref nid);

        _added = true;
    }

    public void UpdateTooltip(string tooltip)
    {
        if (!_added) return;
        var nid = MakeNid();
        nid.uFlags = NIF_TIP;
        SetTip(ref nid, tooltip);
        Shell_NotifyIconW(NIM_MODIFY, ref nid);
    }

    public void ShowBalloon(string title, string text)
    {
        if (!_added) return;
        var nid = MakeNid();
        nid.uFlags = NIF_INFO;
        nid.dwInfoFlags = NIIF_INFO;
        SetInfoTitle(ref nid, title);
        SetInfo(ref nid, text);
        Shell_NotifyIconW(NIM_MODIFY, ref nid);
    }

    public void Remove()
    {
        if (!_added) return;
        var nid = MakeNid();
        Shell_NotifyIconW(NIM_DELETE, ref nid);
        _added = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Remove();
        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Returns true if lParam represents a right-click on the tray icon.
    /// </summary>
    public static bool IsRightClick(IntPtr lParam) =>
        LOWORD(lParam) == WM_RBUTTONUP || LOWORD(lParam) == WM_CONTEXTMENU;

    /// <summary>
    /// Returns true if lParam represents a left double-click on the tray icon.
    /// </summary>
    public static bool IsLeftDoubleClick(IntPtr lParam) =>
        LOWORD(lParam) == WM_LBUTTONDBLCLK;

    /// <summary>
    /// Returns true if the NIN_BALLOONUSERCLICK notification was sent.
    /// </summary>
    public static bool IsBalloonClick(IntPtr lParam) =>
        LOWORD(lParam) == NIN_BALLOONUSERCLICK;

    private NOTIFYICONDATAW MakeNid()
    {
        var nid = new NOTIFYICONDATAW();
        nid.cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>();
        nid.hWnd = _hwnd;
        nid.uID = 1;
        return nid;
    }

    private static ushort LOWORD(IntPtr value) => (ushort)((long)value & 0xFFFF);

    private static unsafe void SetTip(ref NOTIFYICONDATAW nid, string value)
    {
        fixed (char* p = nid.szTip)
        {
            int len = Math.Min(value.Length, 127);
            value.AsSpan(0, len).CopyTo(new Span<char>(p, 128));
            p[len] = '\0';
        }
    }

    private static unsafe void SetInfo(ref NOTIFYICONDATAW nid, string value)
    {
        fixed (char* p = nid.szInfo)
        {
            int len = Math.Min(value.Length, 255);
            value.AsSpan(0, len).CopyTo(new Span<char>(p, 256));
            p[len] = '\0';
        }
    }

    private static unsafe void SetInfoTitle(ref NOTIFYICONDATAW nid, string value)
    {
        fixed (char* p = nid.szInfoTitle)
        {
            int len = Math.Min(value.Length, 63);
            value.AsSpan(0, len).CopyTo(new Span<char>(p, 64));
            p[len] = '\0';
        }
    }

    // --- Constants ---
    private const uint NIM_ADD = 0;
    private const uint NIM_MODIFY = 1;
    private const uint NIM_DELETE = 2;
    private const uint NIM_SETVERSION = 4;
    private const uint NIF_MESSAGE = 0x01;
    private const uint NIF_ICON = 0x02;
    private const uint NIF_TIP = 0x04;
    private const uint NIF_INFO = 0x10;
    private const uint NIIF_INFO = 0x01;
    private const uint NOTIFYICON_VERSION_4 = 4;
    private const ushort WM_RBUTTONUP = 0x0205;
    private const ushort WM_LBUTTONDBLCLK = 0x0203;
    private const ushort WM_CONTEXTMENU = 0x007B;
    private const ushort NIN_BALLOONUSERCLICK = 0x0405;

    // --- Struct ---
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private unsafe struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        public fixed char szTip[128];
        public uint dwState;
        public uint dwStateMask;
        public fixed char szInfo[256];
        public uint uVersion; // union with uTimeout
        public fixed char szInfoTitle[64];
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
