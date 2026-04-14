using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        ConfigureTraceLogging();

        try
        {
            using var messageLoop = new Win32MessageLoop();

            // Defer initialization until the message loop is pumping so hooks
            // and SynchronizationContext-like callbacks work correctly.
            messageLoop.PostDeferredAction(1, () => Initialize(messageLoop));

            messageLoop.Run();
        }
        finally
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }

    private static DesktopPeek? _desktopPeek;
    private static TrayIcon? _trayIcon;
    private static AppUpdater? _appUpdater;

    private static void Initialize(Win32MessageLoop messageLoop)
    {
        var settings = Settings.Load();
        _desktopPeek = new DesktopPeek(settings);
        _appUpdater = new AppUpdater(messageLoop);
        _trayIcon = new TrayIcon(messageLoop, _desktopPeek, _appUpdater, settings, () => messageLoop.Quit());

        if (settings.Enabled)
            _desktopPeek.Start();

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);

            if (_appUpdater is not null)
                await _appUpdater.CheckForUpdatesAsync(interactive: false);
        });
    }

    private static void ConfigureTraceLogging()
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PeekDesktop");

        Directory.CreateDirectory(logDir);

        string logPath = Path.Combine(logDir, "PeekDesktop.log");
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new TextWriterTraceListener(logPath));
        Trace.AutoFlush = true;
    }
}
