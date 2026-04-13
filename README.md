# PeekDesktop 👀

**Click your desktop to peek at it — just like macOS Sonoma.**

PeekDesktop brings macOS Sonoma's "click wallpaper to reveal desktop" feature to Windows 10 and 11. Click empty space on your desktop and all windows minimize. Click any window or the taskbar, and everything comes right back where it was.

<!-- ![PeekDesktop Demo](docs/demo.gif) -->
<!-- TODO: Record demo GIF -->

## Download

📥 **[Download latest release](https://github.com/shanselman/PeekDesktop/releases/latest)**

| File | Platform |
|------|----------|
| `PeekDesktop-x64.exe` | Intel/AMD (most PCs) |
| `PeekDesktop-arm64.exe` | ARM64 (Surface Pro X, Snapdragon, etc.) |

No installation needed. Just run the exe. It lives in your system tray.

## How It Works

1. **Click your desktop wallpaper** (empty space, not on an icon) → all windows minimize
2. **Interact with your desktop** — right-click, open context menus — windows stay hidden
3. **Click any window or the taskbar** → all windows restore to exactly where they were

That's it. It just works.

### Under the Hood

PeekDesktop uses lightweight Windows APIs:

- **`SetWindowsHookEx(WH_MOUSE_LL)`** — low-level mouse hook to detect desktop clicks
- **`WindowFromPoint`** — identifies the window under your cursor
- **`EnumWindows` + `WINDOWPLACEMENT`** — captures exact position and state (including maximized) of every window
- **`SetWinEventHook(EVENT_SYSTEM_FOREGROUND)`** — watches for when you switch back to an app
- **`SetWindowPlacement`** — restores windows to their exact previous positions

No admin rights required. Uses < 5 MB RAM idle.

## System Tray

Right-click the tray icon for options:

- ✅ **Enabled** — toggle the peek feature on/off
- 🔁 **Start with Windows** — launch automatically at login
- ℹ️ **About** — version info
- ❌ **Exit** — quit PeekDesktop

## macOS Sonoma vs PeekDesktop

| Feature | macOS Sonoma | PeekDesktop |
|---------|-------------|-------------|
| Click wallpaper to peek | ✅ | ✅ |
| Restore on app click | ✅ | ✅ |
| Desktop icons accessible | ✅ | ✅ |
| Exact window position restore | ✅ | ✅ |
| System tray control | ❌ | ✅ |
| Multi-monitor support | ✅ | ✅ |
| Start with OS | Login Items | ✅ Registry |
| Smooth animation | ✅ | Coming soon |

## Build from Source

**Requirements:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
git clone https://github.com/shanselman/PeekDesktop.git
cd PeekDesktop
dotnet build src/PeekDesktop/PeekDesktop.csproj
```

### Run it

```bash
dotnet run --project src/PeekDesktop/PeekDesktop.csproj
```

### Publish a self-contained single-file exe

```bash
# For Intel/AMD
dotnet publish src/PeekDesktop/PeekDesktop.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# For ARM64
dotnet publish src/PeekDesktop/PeekDesktop.csproj -c Release -r win-arm64 --self-contained -p:PublishSingleFile=true
```

## Architecture

```
src/PeekDesktop/
├── Program.cs          # Entry point, single-instance mutex
├── DesktopPeek.cs      # Core state machine (Idle ↔ Peeking)
├── MouseHook.cs        # WH_MOUSE_LL global mouse hook
├── FocusWatcher.cs     # EVENT_SYSTEM_FOREGROUND monitor
├── WindowTracker.cs    # Enumerate, minimize, and restore windows
├── DesktopDetector.cs  # Identify Progman/WorkerW desktop windows
├── TrayIcon.cs         # System tray NotifyIcon + context menu
├── Settings.cs         # JSON persistence + registry autostart
└── NativeMethods.cs    # Win32 P/Invoke declarations
```

### State Machine

```
┌──────┐  desktop click   ┌─────────┐
│ Idle │ ───────────────→ │ Peeking │
│      │ ←─────────────── │         │
└──────┘  focus changes    └─────────┘
          to non-desktop
          window
```

## Contributing

PRs welcome! This is a v1 prototype — there's plenty to improve:

- [ ] Smooth minimize/restore animations (slide/fade)
- [ ] Hotkey support (e.g., `Ctrl+F12` to toggle peek)
- [ ] Per-monitor peek (only minimize windows on the clicked monitor)
- [ ] Exclude specific apps from being minimized
- [ ] Better icon (the current one is programmatically generated)
- [ ] Windows 11 widgets area awareness
- [ ] Sound effect on peek/restore

## License

[MIT](LICENSE)
