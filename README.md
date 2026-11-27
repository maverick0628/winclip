<div align="center">
  <img src="brand/app-icon/png/icon-256.png" alt="WinClip logo" width="128" />
</div>
<br />

<div align="center">
  <strong>Smart clipboard actions for Windows. Copy, transform, and paste with intuitive gestures.</strong>
</div>
<br />

<div align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?style=flat-square&logo=windows&logoColor=white" alt="Windows" />
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 8.0" />
  <img src="https://img.shields.io/badge/UI-WPF-blue?style=flat-square" alt="WPF" />
  <img src="https://img.shields.io/badge/Actions-52-green?style=flat-square" alt="52 Built-in Actions" />
</div>
<br />

WinClip is a lightweight system tray utility that enhances the Windows clipboard with gesture-driven popups, contextual text actions, and a theme-aware UI. It runs silently in the background, augmenting your existing workflow without requiring any behavior changes.

> Get the [latest GitHub release](https://github.com/maverick0628/winclip/releases/latest) for x64 and ARM64 installers, or see [INSTALL.md](INSTALL.md) for package-manager and Microsoft Store options.

## How It Works

- **Double-click** a word to copy it instantly. A subtle "Copied" toast appears near your cursor.
- **Double-click again** within 8 seconds to paste it back.
- **Drag-select** text to get a floating toolbar with Copy, Cut, Paste, and an expand arrow for additional actions.
- **Win+Alt+V** opens the clipboard history picker.
- Set `globalHotkey` in settings (for example `Ctrl+Shift+C`) to trigger the clipboard popup anywhere. No popup hotkey is registered by default.

All popups are non-intrusive, theme-aware, and dismiss automatically.

## Features

- **52 Built-in Actions** — Text transforms, encoding, hashing, extraction, and more
- **AI-Powered Actions** — Summarize, Grammar Check, Rephrase, Expand, and Custom Prompt (OpenAI, Claude, or local LM Studio and Ollama, detected automatically on first run). A small preview window shows progress while a request runs.
- **Contextual Filtering** — Actions appear only when relevant to your selected text
- **Gesture Detection** — Low-level mouse hooks detect double-click and drag-select gestures system-wide
- **Themes** — System (auto), Light and Dark
- **Plugin Architecture** — Extend with custom actions via the WinClip SDK
- **Clipboard History** — Searchable picker with pinned entries, stored DPAPI-encrypted across sessions
- **Flyout Search and Pinning** — Type to filter the action flyout, pin up to 3 actions next to Copy/Cut/Paste, and let frequently used actions rise to the top
- **Action Chains** — Define named sequences of actions in settings that run as a single action
- **Image Clipboard** — Save a clipboard image as PNG or decode a QR code or barcode from the tray menu
- **Snippet Manager** — Save and recall frequently used text snippets
- **Auto-Update** — Background update checking with one-click install
- **Process Exclusion** — Skip specific apps where gestures conflict
- **Sound Feedback** — Optional pop sound on clipboard actions
- **Pause/Resume** — Toggle monitoring from the system tray
- **Opt-in Telemetry** — Off by default. When enabled, sends only the action name and app version to Plausible

## Built-in Actions

All 52 actions are included. Contextual actions only appear in the popup when they match your selected text.

| Category | Actions |
|----------|---------|
| **Basics** | Copy, Word Count, Email, Web Search, Define |
| **Text Transform** | UPPERCASE, lowercase, Title Case, PascalCase, camelCase, snake_case, Trim |
| **Encoding** | URL Encode/Decode, Base64 Encode/Decode |
| **JSON** | JSON Prettify, JSON Minify |
| **Line Tools** | Sort A→Z / Z→A, Remove Duplicates, Reverse, Add Line Numbers, Remove Blank Lines, Wrap (80 cols), Unwrap |
| **Hashing** | MD5, SHA1, SHA256 |
| **Conversion** | Timestamp Convert, Number Base Convert, Color Convert (hex/rgb/hsl), CSV ↔ JSON |
| **Extraction** | Smart Extract (emails, URLs, phones, IPs), Strip HTML, Strip Markdown |
| **Text Utilities** | Slugify, Regex Replace, Text Statistics, Markdown Table, QR Code |
| **Productivity** | Translate, Open URL, Clipboard History, Save Snippet, Share to Twitter/X, Send to Blip |
| **AI** | Summarize, Grammar Check, Rephrase, Expand, Custom Prompt |

## Free and Open Source

WinClip is free and open source under the [MIT licence](LICENSE). There is no trial, no licence key and no paid tier. Every feature is available to everyone: 52 built-in actions, AI actions, extensions, clipboard history, snippet manager, themes and auto-updates.

Download installers from [duncansmith.tech/apps/winclip](https://duncansmith.tech/apps/winclip/) or install through a package manager (below). To build from source, see [Build and Test](#build-and-test). Contributions are welcome; read [CONTRIBUTING.md](CONTRIBUTING.md) first.

## Requirements

- Windows 10 version 1903 (build 18362) or later, or Windows 11
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## Installation

### Quick Install

```powershell
# Winget (recommended)
winget install WinClip.WinClip

# Chocolatey
choco install winclip
```

A Scoop manifest is kept in `distribution/scoop/`, but it isn't published to a public bucket yet.

Or download the latest installer from [duncansmith.tech/apps/winclip](https://duncansmith.tech/apps/winclip/).

See [INSTALL.md](INSTALL.md) for detailed instructions.

## Configuration

Settings are persisted as JSON at `%APPDATA%\WinClip\settings.json`. Themes and toggles can also be changed from the system tray right-click menu. See [Docs/Settings.md](Docs/Settings.md) for the full schema.

| Setting | Default | Description |
|---------|---------|-------------|
| `theme` | `System` | Popup theme (System, Light, Dark). Older named themes still load and render as Dark |
| `soundEnabled` | `true` | Play a sound on copy/paste actions |
| `paused` | `false` | Remember pause state across sessions |
| `startWithWindows` | `false` | Launch WinClip on Windows startup |
| `excludedProcesses` | `[]` | Process names to ignore (e.g., `["vmware", "rdclient"]`) |
| `aiProvider` | `null` | AI provider: `"openai"`, `"claude"`, `"lmstudio"`, or `"ollama"` |
| `globalHotkey` | `null` | Hotkey that opens the clipboard popup, e.g. `"Ctrl+Shift+C"` |
| `historyPickerHotkey` | `Win+Alt+V` | Hotkey that opens the clipboard history picker |
| `enableUsageTelemetry` | `false` | Send anonymous action-usage events to Plausible |

## AI Actions

WinClip integrates with OpenAI, Anthropic Claude, and local LM Studio or Ollama for AI-powered text processing. With no provider configured, the first run probes the default local LM Studio and Ollama ports and configures whichever responds. See [Docs/AISetup.md](Docs/AISetup.md) for setup instructions.

## Plugins & Extensibility

WinClip supports custom actions through the WinClip SDK. See [Docs/GettingStarted.md](Docs/GettingStarted.md) and the [Examples/](Examples/) directory.

## Build and Test

```powershell
dotnet build WinClip.sln -c Release
dotnet test WinClip.Tests/WinClip.Tests.csproj -c Release
dotnet test src/WinClip.Tests/WinClip.Tests.csproj -c Release
```

CI runs the same build and both test projects on every push and PR to `master`. Pushing a `v*` tag runs `release.yml`, which publishes x64 and ARM64 builds, signs the binaries and Inno Setup installers with Azure Trusted Signing and creates the GitHub release. `package-release.yml` then updates the Winget, Chocolatey and Scoop manifests.

Auto-update reads `latest.json` from Cloudflare R2 (`AppInfo.UpdateUrl`) rather than the GitHub releases API. A stable tag uploads both installers and the feed to the `winclip-updates` bucket, reads the feed back from the public URL and only then creates the GitHub release. Prerelease tags skip R2. The one-time R2 setup is at the bottom of `release.yml`.

## Architecture

```
WinClip.csproj              Main WPF application (tray icon, popup UI, gesture handler)
src/WinClip.Actions/        52 built-in text transform & utility actions
src/WinClip.Core/           Core services, action registry, extension interfaces
src/WinClip.Extensions/     Extension loading and discovery
src/WinClip.Plugins/        Plugin API, permissions and sandbox
WinClip.SDK/                SDK for building plugins
WinClip.Integrations/       Third-party integrations (Blip)
WinClip.Tests/              Unit tests (main app)
src/WinClip.Tests/          Unit tests (actions & core)
WinClip.Package/            MSIX packaging for Microsoft Store
distribution/               Winget, Chocolatey, Scoop manifests
installer/                  Inno Setup installers (x64, ARM64) and build scripts
Examples/, Templates/       Sample plugins and a plugin project template
```

**Key components:**

- `SelectionMonitorService` — Low-level mouse hook, feeds events to the gesture state machine
- `SelectionDetector` — State machine that classifies mouse gestures (double-click, drag-select)
- `HookManager` — Manages the `WH_MOUSE_LL` hook with automatic watchdog reinstall
- `PopupWindow` — Theme-aware WPF popup with animations, hover effects, and flyout menus
- `ActionRegistry` / `IAction` — Extensible action system with contextual filtering (`IContextualAction`)
- `PluginManager` — Extension discovery and loading
- `SettingsManager` — Thread-safe JSON settings with atomic write-through persistence
- `TrayIcon` — System tray with theme submenu, pause/resume, and update checking

## Troubleshooting

**Popup not appearing:**
Check that WinClip is running (tray icon visible) and not paused. Some apps with aggressive input handling may suppress the hook; add them to `excludedProcesses`.

**Hook stops responding:**
WinClip includes a watchdog that detects when Windows silently removes the hook (common after ~30s of no callbacks) and automatically reinstalls it on the UI thread.

**AI actions not working:**
Ensure an AI provider is configured in Settings > AI Features. See [Docs/AISetup.md](Docs/AISetup.md) for setup and troubleshooting.

## Version History

See [CHANGELOG.md](CHANGELOG.md) for version history.

## License

Copyright (c) 2025-2026 DST Digital, LLC. Released under the [MIT licence](LICENSE). Third-party components and their licences are listed in [NOTICE.md](NOTICE.md).

**Homepage:** https://duncansmith.tech/apps/winclip
