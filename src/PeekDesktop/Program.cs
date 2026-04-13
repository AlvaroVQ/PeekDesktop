using System;
using System.Threading;
using System.Windows.Forms;

namespace PeekDesktop;

public static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    public static void Main()
    {
        _mutex = new Mutex(true, @"Local\PeekDesktop_SingleInstance", out bool isNewInstance);
        if (!isNewInstance)
            return;

        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        try
        {
            Application.Run(new PeekDesktopContext());
        }
        finally
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }
}

/// <summary>
/// Application context for the tray-only app. Defers hook installation
/// until the message loop is running.
/// </summary>
public sealed class PeekDesktopContext : ApplicationContext
{
    private DesktopPeek? _desktopPeek;
    private TrayIcon? _trayIcon;

    public PeekDesktopContext()
    {
        // Defer initialization until the message loop is active so that
        // SynchronizationContext and hooks work correctly.
        var startupTimer = new System.Windows.Forms.Timer { Interval = 1 };
        startupTimer.Tick += (_, _) =>
        {
            startupTimer.Stop();
            startupTimer.Dispose();
            Initialize();
        };
        startupTimer.Start();
    }

    private void Initialize()
    {
        var settings = Settings.Load();
        _desktopPeek = new DesktopPeek();
        _trayIcon = new TrayIcon(_desktopPeek, settings, () => ExitThread());

        if (settings.Enabled)
            _desktopPeek.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _desktopPeek?.Dispose();
            _trayIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
