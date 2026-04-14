using System;
using System.Diagnostics;

namespace PeekDesktop;

internal static class AppDiagnostics
{
    [Conditional("DEBUG")]
    public static void Metric(string message)
    {
        Trace.WriteLine($"[PeekDesktop BENCH {DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    [Conditional("DEBUG")]
    public static void Log(string message)
    {
        Debug.WriteLine($"[PeekDesktop {DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    [Conditional("DEBUG")]
    public static void LogWindow(string prefix, IntPtr hwnd)
    {
        Log($"{prefix}: {NativeMethods.DescribeWindow(hwnd)}");
    }
}
