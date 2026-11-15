# Changelog

All notable changes to WinClip will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.3.0] - 2026-11-14

### Changed
- **WinClip is now free and open source under the MIT licence.** The 30-day trial, licence files and the activation dialog are removed. If your trial had already ended, download this version from https://duncansmith.tech/apps/winclip/ and install it over the old one; an expired copy cannot update itself.
- Refined native Windows theme with flat surfaces, clearer contrast and consistent system typography.
- Popup actions appear together with a short fade, respecting Windows animation preferences.
- Search all settings and action names on one continuous page with persistent section context.
- Correct popup and history placement on secondary and scaled displays.
- Keep popup interaction responsive when another application's accessibility provider stalls.
- Show clipboard-history copy failures instead of reporting success or pasting an old clip.

- **Fewer popups where you didn't select text.** WinClip now ignores a drag or double-click while Ctrl, Alt or the Windows key is held, and a drag where the pointer was never a text cursor, such as moving a window, dragging a scrollbar or rubber-band selecting files. Double-clicks still work with the normal arrow pointer, and apps with their own pointer are never blocked. Clicks that open or close a menu, dropdown or dialog under the pointer are ignored too. Turn these checks off in Settings → General → "Only pop up over text".

- **WinClip stays out of remote-desktop and drawing apps.** In Remote Desktop, Hyper-V, VMware, VirtualBox, Parsec, AnyDesk, TeamViewer and similar, a Ctrl+C would land on the remote machine. In Photoshop, Illustrator, Blender, Krita, GIMP, Paint and similar, drags are painting or moving objects, not selecting text. Alacritty, WezTerm, Tabby, Hyper and Warp are now treated as terminals. These rules ship with WinClip; your own excluded apps still apply on top.

- **Actions read your selection without touching the clipboard** in apps that expose it to Windows accessibility, such as browsers, Office and Notepad. Elsewhere WinClip still copies to read the text, then puts your previous clipboard back, formatting included, unless something else was copied in the meantime. Password-manager clipboards are never saved.

### Fixed
- **WinClip no longer sends Ctrl+C into terminals.** In Windows Terminal, Command Prompt, Git Bash and similar, Ctrl+C with nothing selected stops the running program. WinClip now reads terminal selections through accessibility and does nothing if there is no selection.
- **Double-clicking a word already on the clipboard now shows the popup.** WinClip used to compare the copied text with what was there before, so copying the same word again looked like no copy at all.
- **Actions no longer run on stale clipboard text.** If the copy before an action never lands, WinClip says so instead of running the action on whatever was on the clipboard before.

### Security
- **Updates install only if DST Digital signed them.** The updater accepted any validly signed installer whose signer name merely contained "DST Digital" or "Duncan Smith", and those names came from `appsettings.json`. It now requires the exact Trusted Signing identity of DST Digital, LLC, Microsoft's ID Verified chain and the certificate-profile marker unique to WinClip's signing account, all compiled into the app. Release builds also ignore any update feed URL set in `appsettings.json`, environment variables or command-line arguments.

## [2.2.0] - 2026-09-29

### Changed
- **Licenses are now files, checked offline.** After you buy on Gumroad, the license file arrives by email. Paste all of it, BEGIN and END lines included, into Settings > License. WinClip verifies the signature on your machine and never contacts a licensing server, so an outage on our side can't switch off a paid copy.
- **Gumroad license keys no longer activate.** If you do hold an old key, WinClip clears it on launch and falls back to your trial. Reply to your Gumroad receipt and we'll send you a license file.
- **The 30-day offline grace period is gone.** It covered a licensing server that WinClip no longer talks to. A verified license file works with no network at all.
- The 30-day trial works as before, with the same start date.
- The copyright holder is DST Digital, LLC in the license agreement, the About section in Settings, the app's file properties and the winget and Chocolatey metadata.

### Added
- **Usage statistics now have a switch in Settings.** General → Preferences → "Share anonymous usage statistics". It is off unless you turn it on, and it sends only the name of each action you run and the WinClip version to Plausible, never your text. Before this the setting existed but could only be changed by editing `settings.json`.

### Fixed
- **Enabling or disabling an extension no longer leaves a stale action list.** If the change landed while the popup or Settings was reading the list, the new action could be missing, or the removed one still shown, until another action was registered or removed. The registry now clears its cached list under the same lock that rebuilds it.

## [2.1.2] - 2026-09-17

### Fixed
- **The trial can't be reset by moving the system clock back.** Trial time now counts from the latest time WinClip has seen, saved in settings, so dragging the date back in Windows no longer restarts the 30 days.
- **License activation uses WinClip's Gumroad product ID.** `appsettings.json` sent `winclip`, the short name from the Gumroad URL. Gumroad's license check needs the product ID, so a real key would have been refused.
- **Auto-update now reads a public feed.** WinClip polled the GitHub Releases API on a private repo, which answers 404 to anyone without a token, so no installed copy ever saw an update. It now reads `latest.json` from `winclip-updates.duncansmith.tech` on Cloudflare R2, picks the installer for its own architecture (x64 or ARM64) and checks the download against the SHA-256 in the feed before launching it. Copies on 2.1.1 or older still poll GitHub and need one manual download of this release.
- Store and MSIX installs no longer poll the update feed. Windows updates them, and the feed's Inno Setup installer would have installed a second copy.
- **Update installers are now checked by Windows.** The updater refused every signed installer for two reasons. It trusted signers named "Duncan Smith" or "WinClip", but releases are signed as "DST Digital, LLC". It also rejected any installer whose certificate had expired, and Azure Trusted Signing certificates last three days, so even the right signer would have been refused three days after a build. WinClip now asks Windows (`WinVerifyTrust`) to verify the signature, which checks the file against its signature and honours the signing timestamp, then requires a `DST Digital` or `Duncan Smith` signer.

## [2.1.1] - 2026-05-12

### Changed
- **Trial extended to 30 days** (was 14) — gives prospective customers a full month to evaluate. Existing users with an active trial keep their original start date; the remaining countdown re-calculates against the new 30-day window automatically.
- **Pricing updated to $19.99** (was $24.99) across in-app prompts, distribution manifests, and marketing copy.
- **Microsoft Store distribution: now listed as a free download** with the same 30-day trial gate as the direct/Gumroad channel. Purchase a license at https://duncansmith.gumroad.com/l/winclip when the trial ends; the same key activates whether you installed via Store, winget, choco, scoop, or direct download.

## [2.1.0] - 2026-05-11

### Added
- **Clipboard history picker** — floating, theme-aware window listing pinned and recent clipboard entries with substring search. Default hotkey `Win+Alt+V` (configurable). Pinned items survive ring-buffer eviction via the new `PinnedHistoryStore`.
- **Type-to-filter in the action flyout** — search box appears when 6+ actions are listed; auto-focused on flyout open, arrow keys navigate, Enter activates the first match.
- **Pinned actions in the popup row** — right-click an action in the flyout to pin (or unpin) it; up to 3 pinned action icons render inline next to Copy/Cut/Paste before the "more ▸" button.
- **Usage-aware flyout sort** — once any action has been used at least 3 times, the flyout sorts by frequency (descending) so power-users land on their go-to actions first. Counts are local and on-device.
- **Ollama provider** for AI actions — set `AIProvider=ollama` (or auto-detect; see below). Uses `/api/chat` against `localhost:11434` by default.
- **Local AI auto-detect** — on first run with no `AIProvider` configured, WinClip probes `localhost:11434` (Ollama) and `localhost:1234` (LM Studio); if either responds, the provider + endpoint are configured automatically and a tray notification confirms.
- **Encrypted clipboard history** — `clipboard-history.json` and `pinned-history.json` are now DPAPI-encrypted (CurrentUser scope). Existing plaintext files are read transparently and re-saved encrypted on the next write.
- **Image clipboard support (tray menu)** — new "Image" submenu offers "Save Clipboard Image…" (PNG via SaveFileDialog) and "Decode QR / Barcode" (ZXing — also reads Data Matrix, Aztec, PDF-417, EAN-13, Code 128). Items are disabled when the clipboard holds no image.
- **Opt-in anonymous telemetry** — set `EnableUsageTelemetry=true` in settings to send anonymous action-invocation events to Plausible Analytics. Only the action name + app version are transmitted; never user content. Default is off.
- **Action chains** — define named sequences of actions in `settings.json` under `ActionChains`. Each chain registers as an action in the popup flyout and runs its steps in order, feeding each step's clipboard output to the next step's input.
- **MSIX `.appinstaller` template** at `WinClip.Package/winclip.appinstaller.template.xml` plus operator docs at `Docs/AppInstaller.md`. Enables Windows-native auto-update for the non-Store distribution channel; users install once via `ms-appinstaller:?source=…` and Windows handles updates from there.
- New settings: `HistoryPickerHotkey`, `HistoryMaxEntries` (default 100, up from 50), `ClearHistoryOnExit`, `PinnedActionIds`, `ActionInvocationCounts`, `EnableUsageTelemetry`, `TelemetryDomain`, `ActionChains`.
- `INotificationDispatcher` abstraction for plugin notifications, plus `NullNotificationDispatcher` fallback. Replaces the placeholder `Debug.WriteLine` in `NotificationApiImpl`.
- `PinnedHistoryStoreTests` — 14 unit tests covering add/remove/contains/persistence/concurrency.

### Changed
- `ClipboardHistory` capacity now reads from `SettingsManager.HistoryMaxEntries` (defaults to 100, was hard-coded to 50).
- AI provider auto-detect priority is now Ollama → LM Studio → Claude → OpenAI (was LM Studio → Claude → OpenAI).
- Removed stale `winclip.md` duplicate of the README.

### Notes
- NativeAOT/`PublishTrimmed` was spiked and ruled out: the .NET SDK rejects trimming for projects that reference `System.Windows.Forms` (used for the system tray icon). See `POST_V2_ROADMAP.md` for the closed entry.

---

## [2.0.0] - 2026-03-24

### Added
- Modernized Settings window with updated visual design
- Theme-aware popup and flyout menus with design system tokens
- Configurable auto-copy on drag select (off by default)
- ARM64 installer support

### Changed
- Full UI overhaul: Settings, popup toolbar, and tray menu redesigned
- Consolidated Inno Setup installers (x64 + arm64)
- All distribution manifests updated for v2.0.0
- Homepage URL updated to duncansmith.tech/apps/winclip

### Fixed
- Editable field detection reliability for paste operations
- Nullable warning in global hotkey initialization

---

## [1.2.0] - 2026-03-05

### Added
- Smart context detection: Cut and Paste buttons only appear on editable fields
- AI-powered text actions for intelligent transforms
- UI Automation fallback for improved editable field detection in web browsers

### Fixed
- AI actions now work consistently after configuration changes without requiring a restart
- Paste delay timing for smoother double-click-to-paste workflow
- AI config hot-reload so changes take effect immediately

### Changed
- Refreshed brand icon
- Comprehensive codebase hardening and expanded test coverage

---

## [1.1.0] - 2026-03-01

### Added
- AI-powered text actions (initial release)
- Clipboard history drilldown
- Blip integration fallback
- New application logo

### Changed
- 9 popup themes replacing previous theme set: System (auto), Midnight, Charcoal, Slate, Onyx, Ocean, Forest, Nord, Monokai

---

## [1.0.1] - 2026-02-28

### Changed
- Complete theme overhaul with new popup theme system
- Flyout UI fix
- 10 robustness refinements

---

## [1.0.0] - 2026-02-26

Initial public release.

### Added

#### 52 Built-in Actions

All 52 built-in actions are included. Contextual actions only appear in the popup when they match the selected text.

**Basics:**
Copy, Word Count, Email, Web Search, Define

**Text Transform:**
UPPERCASE, lowercase, Title Case

**Encoding:**
URL Encode, URL Decode, Base64 Encode, Base64 Decode

**JSON:**
JSON Prettify, JSON Minify

**Line Tools:**
Sort Lines A-Z, Sort Lines Z-A, Remove Duplicate Lines, Reverse Lines, Add Line Numbers, Remove Blank Lines, Wrap Lines (80 cols), Unwrap to Single Line

**Hashing:**
Hash MD5, Hash SHA1, Hash SHA256

**Conversion:**
Timestamp Convert, Number Base Convert, Color Convert (hex/rgb/hsl), CSV-JSON Convert

**Extraction:**
Smart Extract (emails, URLs, phones, IPs), Strip HTML, Strip Markdown

**Text Utilities:**
Slugify, Regex Replace, Text Statistics, Markdown Table, QR Code

**Productivity:**
Translate (Google Translate), Open URL, Send to Blip

#### Gesture Detection
- Low-level mouse hook (`WH_MOUSE_LL`) detects double-click and drag-select gestures system-wide
- `SelectionDetector` state machine classifies mouse gestures with configurable timing thresholds
- `HookManager` with automatic watchdog reinstall when Windows silently drops the hook
- Global hotkey `Ctrl+Shift+C` to manually trigger the clipboard popup

#### Popup UI
- Floating toolbar on drag-select with Copy, Cut, Paste buttons and expand arrow
- Subtle "Copied" toast on double-click copy, appearing near the cursor
- Double-click again within 8 seconds to paste back
- Theme-aware WPF popup with scale, slide, and opacity animations
- Hover pauses auto-dismiss; popups dismiss automatically after timeout

#### Visual Themes
- 6 built-in themes: System (auto), Light, Dark, Grey, Translucent, Glassmorphic
- Translucent and Glassmorphic use Windows acrylic blur (Win10 1803+ / Win11)
- Graceful degradation to solid background on older builds
- Theme selectable from system tray right-click menu

#### Contextual Actions
- Expand arrow reveals text transforms filtered by content type
- 18 contextual actions auto-detect content (JSON, URLs, HTML, timestamps, colors, etc.)
- `IContextualAction` interface for relevance-based filtering

#### Licensing
- $19.99 one-time purchase via Gumroad — all features included, 30-day free trial
- Gumroad-backed license activation with 30-day offline grace period
- Per-device activation tracking with deactivation for device transfers
- License state persisted locally at `%APPDATA%\WinClip\settings.json`

#### Plugin SDK
- `IAction` interface and `ActionRegistry` for extensible actions
- `IContextualAction` interface for content-aware popup filtering
- `WinClip.SDK` NuGet package for third-party plugin development
- `dotnet new winclip-plugin` template for rapid plugin scaffolding
- Example plugins: HelloWorld, TextTransform, WebSearch
- Extension loading with plugin isolation

#### Clipboard History
- 50-entry ring buffer tracking recently copied items

#### System Tray
- Pause/resume monitoring toggle
- Theme submenu for quick switching
- Auto-update checking with one-click install
- Process exclusion list for apps where gestures conflict

#### Blip Integration
- Send selected text to Blip when Blip desktop app is installed
- Auto-detected via `IBlipDetector`; action only appears when Blip is available

#### Infrastructure
- .NET 8.0 / WPF with Windows Forms interop for system tray
- MSIX packaging for Microsoft Store (`WinClip.Package`)
- Inno Setup installer scripts for direct download
- Winget, Chocolatey, and Scoop distribution manifests
- CI/CD for build, test, release, and package manager updates
- Thread-safe JSON settings with live persistence at `%APPDATA%\WinClip\settings.json`
- File-based logging for tray app debugging

### Security
- URL validation on all `Process.Start` calls (http/https only)
- `OpenUrlAction` rejects non-http protocols (mailto:, ftp:, file:, etc.)
- STA thread enforcement for clipboard operations
- 2-second regex timeout in `RegexReplaceAction` to prevent backtracking attacks
- Isolated `AssemblyLoadContext` for plugin sandboxing

---

[Unreleased]: https://duncansmith.tech/apps/winclip/changelog
[2.2.0]: https://duncansmith.tech/apps/winclip/changelog
[2.1.2]: https://duncansmith.tech/apps/winclip/changelog
[2.0.0]: https://duncansmith.tech/apps/winclip/changelog
[1.2.0]: https://duncansmith.tech/apps/winclip/changelog
[1.1.0]: https://duncansmith.tech/apps/winclip/changelog
[1.0.1]: https://duncansmith.tech/apps/winclip/changelog
[1.0.0]: https://duncansmith.tech/apps/winclip/changelog
