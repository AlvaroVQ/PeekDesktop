using System;

namespace PeekDesktop;

/// <summary>
/// Core orchestrator for the peek-desktop feature.
/// State machine with two states: Idle and Peeking.
///
///   Idle → Peeking:  user clicks the desktop surface
///   Peeking → Idle:  a non-desktop window gains foreground focus
/// </summary>
public sealed class DesktopPeek : IDisposable
{
    private readonly MouseHook _mouseHook = new();
    private readonly FocusWatcher _focusWatcher = new();
    private readonly WindowTracker _windowTracker = new();

    private bool _isPeeking;
    private bool _isTransitioning; // suppresses events during minimize/restore

    public bool IsEnabled { get; set; } = true;
    public bool IsPeeking => _isPeeking;

    public DesktopPeek()
    {
        _mouseHook.DesktopClicked += OnDesktopClicked;
        _focusWatcher.FocusChanged += OnFocusChanged;
    }

    public void Start()
    {
        _mouseHook.Install();
        _focusWatcher.Start();
    }

    public void Stop()
    {
        _mouseHook.Uninstall();
        _focusWatcher.Stop();

        if (_isPeeking)
            RestoreWindows();
    }

    private void OnDesktopClicked(object? sender, EventArgs e)
    {
        if (!IsEnabled || _isPeeking || _isTransitioning)
            return;

        PeekDesktopNow();
    }

    private void OnFocusChanged(object? sender, FocusChangedEventArgs e)
    {
        if (!_isPeeking || _isTransitioning)
            return;

        // If the new foreground is still the desktop or transient desktop UI, stay peeking
        if (DesktopDetector.IsDesktopWindow(e.ForegroundWindow))
            return;

        RestoreWindows();
    }

    private void PeekDesktopNow()
    {
        _isTransitioning = true;
        try
        {
            _windowTracker.CaptureWindows();
            if (_windowTracker.HasWindows)
            {
                _windowTracker.MinimizeAll();
                _isPeeking = true;
            }
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private void RestoreWindows()
    {
        _isTransitioning = true;
        try
        {
            _windowTracker.RestoreAll();
            _isPeeking = false;
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    public void Dispose()
    {
        Stop();
        _mouseHook.Dispose();
        _focusWatcher.Dispose();
    }
}
