using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WinClip.Actions;
using WinClip.Core.Services;
using WinClip.Extensions;
using WinClip.Models;
using WinClip.Native;
using WinClip.Services;
using WinClip.UI;
using WinClip.UI.Styles;

namespace WinClip;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<App> _logger;
    private readonly ISelectionMonitorService _selectionMonitor;
    private readonly IActionRegistry _actionRegistry;
    private readonly PluginManager? _pluginManager;
    private readonly SettingsManager? _settings;
    private readonly ClipboardHistory? _clipboardHistory;

    private PopupWindow? _currentPopup;
    private TrayIcon? _trayIcon;
    private GlobalHotkeyService? _hotkeyService;
    private GlobalHotkeyService? _historyPickerHotkeyService;
    private bool _processing;
    private long _selectionRequest;
    private CancellationTokenSource? _actionCts;

    // Double-click state: first double-click copies, second double-click pastes
    private string? _copiedText;
    private DateTime _copiedAt = DateTime.MinValue;
    private static readonly TimeSpan PasteWindow = TimeSpan.FromSeconds(8);

    // Cached Marshal.SizeOf — avoids repeated reflection on every SendInput call
    private static readonly int InputStructSize = Marshal.SizeOf<NativeMethods.INPUT>();

    // Enabled actions set — resolved fresh from settings each time to pick up changes

    // Parameterless constructor required by WPF XAML code generation (App.g.cs).
    public App()
    {
        _serviceProvider = null!;
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<App>.Instance;
        _selectionMonitor = null!;
        _actionRegistry = null!;
    }

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<App>>();
        _selectionMonitor = serviceProvider.GetRequiredService<ISelectionMonitorService>();
        _actionRegistry = serviceProvider.GetRequiredService<IActionRegistry>();
        _pluginManager = serviceProvider.GetService<PluginManager>();
        _settings = serviceProvider.GetService<SettingsManager>();
        _clipboardHistory = serviceProvider.GetService<ClipboardHistory>();
        InitializeComponent();
    }

    /// <summary>
    /// Sound on/off — backed by SettingsManager so it persists across sessions.
    /// </summary>
    public bool SoundEnabled
    {
        get => _settings?.SoundEnabled ?? true;
        set { if (_settings != null) _settings.SoundEnabled = value; }
    }

    /// <summary>
    /// The settings manager instance, exposed so TrayIcon can read/write settings.
    /// </summary>
    public SettingsManager? Settings => _settings;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppThemeManager.Initialize(this);
        ApplyWpfUiTheme();

        // ── Global exception handlers — keep the app alive and log all failures ──
        DispatcherUnhandledException += (_, args) =>
        {
            _logger.LogError(args.Exception, "Unhandled dispatcher exception");
            args.Handled = true; // Prevent crash — keep running
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                _logger.LogCritical(ex, "Unhandled AppDomain exception (IsTerminating={Term})", args.IsTerminating);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger.LogError(args.Exception, "Unobserved task exception");
            args.SetObserved(); // Prevent process termination
        };

        if (_selectionMonitor == null!)
        {
            MessageBox.Show("WinClip must be started via Program.Main (DI host).",
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        _logger.LogInformation("WinClip starting...");

        _selectionMonitor.SelectionDetected += OnSelectionDetected;

        // If the user left the app paused last session, don't start the monitor
        bool startPaused = _settings?.Paused ?? false;

        if (!startPaused)
        {
            try
            {
                _selectionMonitor.Start();
                _logger.LogInformation("Selection monitor started.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start selection monitor.");
                MessageBox.Show("Failed to start selection monitor. The application will exit.",
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }
        }
        else
        {
            _logger.LogInformation("Selection monitor starting paused (from saved settings).");
        }

        // ── Initialize action profile on first launch ──
        if (_settings != null && _settings.EnabledActions.Count == 0)
        {
            var preset = ActionProfilePresets.GetPresetActions(_settings.ActiveProfile);
            _settings.EnabledActions = preset.ToList();
        }

        var updateService = _serviceProvider.GetService<IUpdateService>();
        var trayLogger = _serviceProvider.GetService<ILogger<TrayIcon>>();
        var snippetStore = _serviceProvider.GetService<SnippetStore>();
        _trayIcon = new TrayIcon(_selectionMonitor, updateService, _settings, trayLogger, _actionRegistry, snippetStore);

        // Global hotkey — only register if the user has configured one in settings.
        // Default is no hotkey to avoid conflicts with other applications.
        _hotkeyService = _serviceProvider.GetService<GlobalHotkeyService>();
        if (_hotkeyService != null)
        {
            _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;

            var globalHotkey = _settings?.GlobalHotkey;
            if (!string.IsNullOrWhiteSpace(globalHotkey))
            {
                if (TryParseHotkey(globalHotkey, out uint modifiers, out uint vk))
                {
                    if (!_hotkeyService.Register(modifiers, vk))
                    {
                        _trayIcon?.ShowNotification(
                            "WinClip",
                            $"Could not register {globalHotkey} hotkey — another app may be using it.",
                            System.Windows.Forms.ToolTipIcon.Warning);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid global hotkey format: \"{Hotkey}\". Expected format: Ctrl+Shift+C", globalHotkey);
                }
            }
        }

        // Clipboard history picker hotkey (separate registration so the picker and
        // popup hotkeys don't collide on a single HOTKEY_ID).
        var pickerHotkey = _settings?.HistoryPickerHotkey;
        if (!string.IsNullOrWhiteSpace(pickerHotkey))
        {
            var pickerLogger = _serviceProvider.GetService<ILogger<GlobalHotkeyService>>();
            _historyPickerHotkeyService = new GlobalHotkeyService(pickerLogger);
            _historyPickerHotkeyService.HotkeyPressed += OnHistoryPickerHotkeyPressed;

            if (TryParseHotkey(pickerHotkey, out uint pickerMods, out uint pickerVk))
            {
                if (!_historyPickerHotkeyService.Register(pickerMods, pickerVk))
                {
                    _trayIcon?.ShowNotification(
                        "WinClip",
                        $"Could not register {pickerHotkey} for clipboard history — another app may be using it.",
                        System.Windows.Forms.ToolTipIcon.Warning);
                }
            }
            else
            {
                _logger.LogWarning("Invalid history picker hotkey format: \"{Hotkey}\". Expected format: Win+Alt+V", pickerHotkey);
            }
        }

        _ = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
        {
            Dispatcher.BeginInvoke(() => _trayIcon?.CheckForUpdateInBackgroundAsync());
        });

        _ = Task.Run(AutoDetectLocalAIProviderAsync);
    }

    /// <summary>
    /// Apply the user's Theme setting across all three theme surfaces:
    /// (1) WinClip's own resource dictionary (drives Settings window cards/text),
    /// (2) WPF-UI's Fluent palette (drives ui:Button, ui:ToggleSwitch, ui:TitleBar),
    /// (3) Brand-teal accent override (#2DD4BF per DESIGN.md).
    /// Called at startup and whenever the user changes the theme picker.
    /// </summary>
    public void ApplyWpfUiTheme()
    {
        var kind = _settings?.Theme ?? PopupThemeKind.System;

        // (1) Drive WinClip's own light/dark resource dictionary
        AppThemeManager.Apply(kind);

        // (2) Resolve System → effective Light/Dark for WPF-UI
        var effective = kind;
        if (effective == PopupThemeKind.System)
            effective = ThemeDetector.IsDarkMode() ? PopupThemeKind.Dark : PopupThemeKind.Light;

        var target = effective == PopupThemeKind.Light
            ? Wpf.Ui.Appearance.ApplicationTheme.Light
            : Wpf.Ui.Appearance.ApplicationTheme.Dark;

        try
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(target);
            var teal = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(PopupTheme.GetColors(effective).AccentColor);
            Wpf.Ui.Appearance.ApplicationAccentColorManager.Apply(teal, target);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "WPF-UI theme apply failed (non-fatal)");
        }
    }

    /// <summary>
    /// If the user hasn't picked an AI provider yet, probe localhost for Ollama / LM Studio.
    /// On hit: auto-configure the provider + endpoint and surface a tray notification so the
    /// user knows AI actions are now available.
    /// </summary>
    private async Task AutoDetectLocalAIProviderAsync()
    {
        try
        {
            if (_settings == null) return;
            if (!string.IsNullOrWhiteSpace(_settings.AIProvider)) return; // already configured

            var detector = _serviceProvider.GetService<LocalAIProviderDetector>();
            if (detector == null) return;

            // Brief delay so the probe doesn't compete with startup work
            await Task.Delay(TimeSpan.FromSeconds(2));
            var detected = await detector.DetectAsync();

            string? providerName = null;
            string? endpoint = null;
            switch (detected)
            {
                case LocalAIProviderDetector.Provider.Ollama:
                    providerName = "ollama";
                    endpoint = "http://localhost:11434";
                    break;
                case LocalAIProviderDetector.Provider.LMStudio:
                    providerName = "lmstudio";
                    endpoint = "http://localhost:1234";
                    break;
            }

            if (providerName == null || endpoint == null) return;

            _settings.AIProvider = providerName;
            _settings.AIEndpoint = endpoint;

            try
            {
                _settings.SyncAISettingsToAppSettings();
                var configRoot = _serviceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>()
                    as Microsoft.Extensions.Configuration.IConfigurationRoot;
                configRoot?.Reload();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to sync auto-detected AI settings.");
            }

            await Dispatcher.InvokeAsync(() =>
            {
                _trayIcon?.ShowNotification(
                    "WinClip — AI actions enabled",
                    $"Detected {(providerName == "ollama" ? "Ollama" : "LM Studio")} at {endpoint}. AI actions are now available.",
                    System.Windows.Forms.ToolTipIcon.Info);
            });

            _logger.LogInformation("Auto-configured AI provider {Provider} at {Endpoint}", providerName, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Local AI provider auto-detect failed (non-fatal).");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger.LogInformation("WinClip shutting down...");

        // Persist pause state
        if (_settings != null)
            _settings.Paused = !_selectionMonitor.IsRunning;

        try
        {
            if (_selectionMonitor.IsRunning)
                _selectionMonitor.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping selection monitor.");
        }
        _selectionMonitor.SelectionDetected -= OnSelectionDetected;
        _hotkeyService?.Dispose();
        _historyPickerHotkeyService?.Dispose();
        _pluginManager?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Core handler: fired when SelectionMonitorService detects a gesture.
    ///   • Double-click → auto-copy word, show "Copied" toast, pop sound
    ///   • Drag-select (highlight) → show Copy / Cut buttons (prompts copy)
    ///   • Single-click → show Paste button if clipboard has text
    /// </summary>
    private void OnSelectionDetected(object? sender, SelectionEvent e)
    {
        var request = Interlocked.Increment(ref _selectionRequest);
        Dispatcher.BeginInvoke(async () =>
        {
            var target = NativeMethods.GetForegroundWindow();
            bool IsCurrent() => request == Volatile.Read(ref _selectionRequest)
                && target == NativeMethods.GetForegroundWindow();
            if (!IsCurrent()) return;
            // ── Gate: shipped per-app policy, Ctrl/Alt/Win held, or a cursor that can't be over text ──
            var policyHwnd = NativeMethods.GetForegroundWindow();
            var policy = SelectionPolicy.Default.Evaluate(new PolicyContext(
                WindowSnapshot.ProcessName(policyHwnd), WindowSnapshot.ClassName(policyHwnd), e.Gesture));
            var rejectReason = policy.HasFlag(PolicyEffects.Deny)
                ? "app denied by selection policy"
                : SelectionGate.RejectReason(e, _settings?.RequireTextCursor ?? true,
                    policy.HasFlag(PolicyEffects.AllowArrowDrag));
            if (rejectReason != null)
            {
                _logger.LogDebug("{Gesture} ignored: {Reason}", e.Gesture, rejectReason);
                // A gesture elsewhere still means the user moved on
                if (!_processing) DismissPopup();
                return;
            }

            // ── Fast-paste bypass: allow paste even during prior event processing ──
            // This eliminates the ~1s delay when double-clicking to paste
            // while the first double-click's clipboard polling is still running.
            // Only paste if the focused field is editable — otherwise ignore.
            if (_processing)
            {
                if (e.IsDoubleClick && _copiedText != null && (DateTime.UtcNow - _copiedAt) < PasteWindow
                    && EditableFieldDetector.HasCaret())
                {
                    var fastText = _copiedText;
                    _copiedText = null;
                    _copiedAt = DateTime.MinValue;
                    DismissPopup();
                    try { WinClip.Actions.ClipboardService.Set(fastText); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Clipboard SetText failed (fast paste)"); }
                    SimulatePaste();
                    PlaySound();
                }
                return;
            }
            _processing = true;
            var started = Stopwatch.GetTimestamp();

            try
            {
                // Skip our own windows and excluded processes
                var fgHwnd = NativeMethods.GetForegroundWindow();
                if (IsOwnWindow(fgHwnd) || IsExcludedProcess(fgHwnd))
                    return;

                DismissPopup();

                if (e.IsDoubleClick)
                {
                    // ── Second double-click within PasteWindow ──
                    // Paste if in an editable field, otherwise re-copy the new word
                    if (_copiedText != null && (DateTime.UtcNow - _copiedAt) < PasteWindow)
                    {
                        var pasteEditability = await EditableFieldDetector.IsFocusedControlEditableAsync(e.CursorX, e.CursorY);
                        if (!IsCurrent() || !pasteEditability.IsCurrent) return;
                        bool canPaste = pasteEditability.IsEditable;
                        if (canPaste)
                        {
                            var textToPaste = _copiedText;
                            _copiedText = null;
                            _copiedAt = DateTime.MinValue;

                            try { WinClip.Actions.ClipboardService.Set(textToPaste); }
                            catch (Exception ex) { _logger.LogWarning(ex, "Clipboard SetText failed (paste)"); }
                            SimulatePaste();
                            PlaySound();

                            var pastePopup = CreatePopup();
                            pastePopup.Dismissed += OnPopupDismissed;
                            _currentPopup = pastePopup;
                            pastePopup.ShowPasted(e.CursorX, e.CursorY);
                            return;
                        }
                        // Not editable — clear paste window and fall through to re-copy
                        _copiedText = null;
                        _copiedAt = DateTime.MinValue;
                    }

                    // ── First double-click → copy the selected word ──
                    // If the clipboard never changes, the double-click selected nothing — ignore
                    var newText = await CaptureSelectionAsync(CapturePurpose.Copy, ClipboardChangeWaiter.DefaultDelaysMs);
                    if (!IsCurrent()) return;
                    if (newText == null)
                        return;

                    // Remember what we copied and track in history
                    _copiedText = newText;
                    _copiedAt = DateTime.UtcNow;
                    _clipboardHistory?.Add(newText);

                    // "Copied" toast — show Cut/Paste only when in an editable field
                    var editability = await EditableFieldDetector.IsFocusedControlEditableAsync(e.CursorX, e.CursorY);
                    if (!IsCurrent() || !editability.IsCurrent) return;
                    bool isEditable = editability.IsEditable;
                    var enabledActions = GetEnabledActions();
                    var popup = CreatePopup();

                    popup.PasteRequested += () => SimulatePaste();
                    popup.CutRequested += () =>
                    {
                        SimulateCut();
                        _copiedText = newText;
                        _copiedAt = DateTime.UtcNow;
                    };
                    popup.ActionRequested += async (action) =>
                    {
                        var textToUse = !string.IsNullOrWhiteSpace(newText) ? newText : SafeReadClipboard();
                        if (string.IsNullOrWhiteSpace(textToUse))
                        {
                            _logger.LogWarning("ActionRequested fired with empty text — surfacing error to user");
                            _currentPopup?.ShowActionResult(false, $"{action.Name}: no text captured — try selecting again");
                            return;
                        }
                        await ExecuteAction(action, textToUse);
                    };

                    popup.Dismissed += OnPopupDismissed;
                    _currentPopup = popup;
                    popup.ShowCopied(e.CursorX, e.CursorY, showPaste: isEditable, actions: enabledActions, selectedText: newText);
                    PlaySound();
                }
                else if (e.IsDragSelect)
                {
                    // ── Drag-select (highlight) ──

                    var enabledActions = GetEnabledActions();

                    // ── Auto-copy mode: immediately copy and show "Copied" toast ──
                    if (_settings?.AutoCopyOnDragSelect == true)
                    {
                        var newText = await CaptureSelectionAsync(CapturePurpose.Copy, ClipboardChangeWaiter.DefaultDelaysMs);
                        if (!IsCurrent()) return;
                        if (newText != null)
                        {
                            _copiedText = newText;
                            _copiedAt = DateTime.UtcNow;
                            _clipboardHistory?.Add(newText);

                            var editability = await EditableFieldDetector.IsFocusedControlEditableAsync(e.CursorX, e.CursorY);
                            if (!IsCurrent() || !editability.IsCurrent) return;
                            bool isEditable = editability.IsEditable;
                            var popup = CreatePopup();
                            popup.PasteRequested += () => SimulatePaste();
                            popup.CutRequested += () =>
                            {
                                SimulateCut();
                                _copiedText = newText;
                                _copiedAt = DateTime.UtcNow;
                            };
                            popup.ActionRequested += async (action) =>
                            {
                                var textToUse = !string.IsNullOrWhiteSpace(newText) ? newText : SafeReadClipboard();
                                if (string.IsNullOrWhiteSpace(textToUse))
                                {
                                    _logger.LogWarning("ActionRequested fired with empty text — surfacing error to user");
                                    _currentPopup?.ShowActionResult(false, $"{action.Name}: no text captured — try selecting again");
                                    return;
                                }
                                await ExecuteAction(action, textToUse);
                            };
                            popup.Dismissed += OnPopupDismissed;
                            _currentPopup = popup;
                            popup.ShowCopied(e.CursorX, e.CursorY, showPaste: isEditable, actions: enabledActions, selectedText: newText);
                            // No sound on drag-select — sound only plays on double-click
                            return;
                        }
                        // Auto-copy failed (nothing selected) — fall through to normal Copy/Cut popup
                    }

                    // ── Normal mode: show Copy / Cut / Paste buttons ──
                    {
                        var editability = await EditableFieldDetector.IsFocusedControlEditableAsync(e.CursorX, e.CursorY);
                        if (!IsCurrent() || !editability.IsCurrent) return;
                        bool isEditable = editability.IsEditable;
                        var popup = CreatePopup();

                        popup.CopyRequested += async () =>
                        {
                            // Probe clipboard so we track what was copied
                            var text = await CaptureSelectionAsync(CapturePurpose.Copy, ButtonCopyDelaysMs);
                            if (text != null)
                            {
                                _copiedText = text;
                                _copiedAt = DateTime.UtcNow;
                                _clipboardHistory?.Add(text);
                            }
                        };

                        popup.CutRequested += async () =>
                        {
                            var text = await CaptureSelectionAsync(CapturePurpose.Cut, ButtonCopyDelaysMs);
                            if (text != null)
                            {
                                _copiedText = text;
                                _copiedAt = DateTime.UtcNow;
                                _clipboardHistory?.Add(text);
                            }
                        };

                        popup.PasteRequested += () =>
                        {
                            SimulatePaste();
                        };

                        popup.ActionRequested += async (action) =>
                        {
                            // ALWAYS capture the *current* selection rather than whatever stale
                            // text happens to be in the clipboard from a previous action.
                            // (Previous bug: stale Summarize result was getting re-summarized
                            // instead of the new selection.) UI Automation reads it without
                            // touching the clipboard; otherwise Ctrl+C, waiting on the sequence
                            // number so a copy that never lands is an error, not stale text.
                            var text = await CaptureSelectionAsync(CapturePurpose.ReadText, ButtonCopyDelaysMs);

                            if (string.IsNullOrWhiteSpace(text))
                            {
                                _logger.LogWarning("ActionRequested: no text after Copy — surfacing error");
                                _currentPopup?.ShowActionResult(false, $"{action.Name}: no text selected — try selecting again");
                                return;
                            }
                            await ExecuteAction(action, text);
                        };

                        popup.Dismissed += OnPopupDismissed;
                        _currentPopup = popup;
                        bool hasClipboardText = false;
                        try { hasClipboardText = Clipboard.ContainsText() && !string.IsNullOrWhiteSpace(Clipboard.GetText()); }
                        catch (Exception ex) { _logger.LogWarning(ex, "Clipboard check failed (paste check)"); }

                        popup.ShowSelection(e.CursorX, e.CursorY, clipboardHasText: hasClipboardText && isEditable, enabledActions, isEditable: isEditable);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in selection handler");
            }
            finally
            {
                _processing = false;
                _logger.LogDebug("Selection UI preparation ({Gesture}) took {ElapsedMs:F1} ms",
                    e.Gesture, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
        }, DispatcherPriority.Normal);
    }

    /// <summary>
    /// Executes an action on the given text and shows feedback in the current popup.
    /// </summary>
    /// <summary>
    /// Reads the clipboard with a brief retry. Used as a fallback when the popup's
    /// captured text was empty (the original copy may have raced with the popup show).
    /// </summary>
    private string? SafeReadClipboard()
    {
        for (int i = 0; i < 5; i++)
        {
            try { return Clipboard.GetText(); }
            catch (Exception ex)
            {
                if (i == 4) _logger.LogDebug(ex, "SafeReadClipboard exhausted retries");
                System.Threading.Thread.Sleep(40);
            }
        }
        return null;
    }

    private async Task ExecuteAction(IAction action, string text)
    {
        // Trace every dispatch so AI failures show up clearly in logs even when they
        // bubble up as non-exception failure results (e.g., empty content from API).
        text ??= string.Empty;
        _logger.LogInformation("Action dispatch: {Name} (textLen={Len})", action.Name, text.Length);

        // Surface a small floating preview window for AI actions — they take 5–30s
        // and the popup's "Processing..." gives no signal during the wait. Once the
        // preview is up it owns the entire feedback lifecycle, so dismiss the popup's
        // redundant Processing pill to keep the UI clean.
        var isAi = action is WinClip.Actions.AI.AIActionBase;
        UI.AIPreviewWindow? preview = null;
        if (isAi)
        {
            try
            {
                preview = UI.AIPreviewWindow.ShowProcessing(action.Name, _settings?.Theme ?? PopupThemeKind.System);
                _logger.LogInformation("AI preview opened for {Name} at ({Left:0},{Top:0})",
                    action.Name, preview.Left, preview.Top);
                // Hand off to the preview window — pop the redundant Processing pill.
                DismissPopup();
            }
            catch (Exception ex) { _logger.LogWarning(ex, "AI preview window open failed (non-fatal)"); }
        }

        // Local usage counter — drives flyout sort order. Purely on-device.
        try { _settings?.IncrementActionUsage(action.Name); }
        catch (Exception ex) { _logger.LogDebug(ex, "Failed to increment usage counter for {Name}", action.Name); }

        // Opt-in remote telemetry (no-op unless EnableUsageTelemetry is true).
        try { _serviceProvider.GetService<TelemetryClient>()?.TrackActionInvoked(action.Name); }
        catch (Exception ex) { _logger.LogDebug(ex, "Telemetry track failed for {Name} (suppressed)", action.Name); }

        try
        {
            var result = await action.ExecuteAsync(text).WaitAsync(TimeSpan.FromSeconds(60));
            if (result.Success)
            {
                _logger.LogInformation("Action result: {Name} succeeded ({Msg})", action.Name,
                    result.Message?.StartsWith("AI_RESULT_NO_CLIPBOARD::") == true
                        ? "<AI result, clipboard blocked — manual-copy fallback>"
                        : result.Message ?? "<no message>");

                // AI clipboard fallback: result is on hand but the clipboard write was
                // blocked by another app. Surface the result in the preview window with
                // a manual Copy button so the AI work isn't lost.
                if (result.Message?.StartsWith("AI_RESULT_NO_CLIPBOARD::") == true)
                {
                    var aiResult = result.Message["AI_RESULT_NO_CLIPBOARD::".Length..];
                    preview?.ShowResultForManualCopy(action.Name, aiResult);
                    _currentPopup?.ShowActionResult(false, $"{action.Name}: clipboard busy, see preview window");
                    return;
                }

                // Auto-paste only when the action explicitly requests it (in-place transforms)
                if (result.AutoPaste)
                    SimulatePaste();

                _currentPopup?.ShowActionResult(true, result.Message ?? action.Name);
                preview?.ShowSuccess(action.Name, result.Message);
            }
            else
            {
                _logger.LogWarning("Action result: {Name} failed ({Msg})", action.Name, result.Message ?? "<no message>");
                _currentPopup?.ShowActionResult(false, result.Message ?? "Failed");
                preview?.ShowError(action.Name, result.Message ?? "Unknown error");
            }
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Action '{Name}' timed out after 30 seconds", action.Name);
            _currentPopup?.ShowActionResult(false, $"{action.Name} timed out");
            preview?.ShowError(action.Name, "Timed out after 30 seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Action '{Name}' threw", action.Name);
            // Surface the actual error message to the user — generic "X failed" hides
            // common issues like stale model IDs, expired keys, network errors. Trim
            // long messages so the popup doesn't overflow.
            var msg = (ex as InvalidOperationException)?.Message ?? ex.Message;
            if (msg.Length > 120) msg = msg[..117] + "...";
            _currentPopup?.ShowActionResult(false, $"{action.Name}: {msg}");
            preview?.ShowError(action.Name, msg);
        }
    }

    // ── Global hotkey handler ─────────────────────────────────────────

    /// <summary>
    /// Triggered by the configured clipboard-history hotkey (default: Win+Alt+V).
    /// Toggles the floating <see cref="HistoryPickerWindow"/>.
    /// </summary>
    private void OnHistoryPickerHotkeyPressed()
    {
        try
        {
            HistoryPickerWindow.Toggle(_serviceProvider);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to toggle clipboard history picker.");
        }
    }

    /// <summary>
    /// Triggered by Ctrl+Shift+C. Reads the current clipboard text and shows the popup
    /// at the cursor position — useful when automatic gesture detection doesn't fire.
    /// </summary>
    private void OnGlobalHotkeyPressed()
    {
        Dispatcher.BeginInvoke(async () =>
        {
            if (_processing) return;
            _processing = true;
            var request = Interlocked.Increment(ref _selectionRequest);
            var target = NativeMethods.GetForegroundWindow();
            bool IsCurrent() => request == Volatile.Read(ref _selectionRequest)
                && target == NativeMethods.GetForegroundWindow();

            try
            {
                _logger.LogDebug("Global hotkey pressed — manual clipboard popup.");
                DismissPopup();

                // First copy the current selection (the popup says "Copied"), then fall back
                // to whatever is already on the clipboard
                string? text = await CaptureSelectionAsync(CapturePurpose.Copy, ButtonCopyDelaysMs)
                    ?? TryReadClipboardText();
                if (!IsCurrent()) return;

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogDebug("No text on clipboard for hotkey popup.");
                    return;
                }

                // Get cursor position
                GetCursorPos(out var cursorPoint);

                var editability = await EditableFieldDetector.IsFocusedControlEditableAsync(cursorPoint.X, cursorPoint.Y);
                if (!IsCurrent() || !editability.IsCurrent) return;
                bool isEditable = editability.IsEditable;
                var actions = GetEnabledActions();
                var popup = CreatePopup();

                popup.PasteRequested += () =>
                {
                    try { WinClip.Actions.ClipboardService.Set(text); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Clipboard SetText failed (hotkey paste)"); }
                    SimulatePaste();
                };

                popup.ActionRequested += async (action) =>
                {
                    await ExecuteAction(action, text);
                };

                popup.Dismissed += OnPopupDismissed;
                _currentPopup = popup;
                // Show Cut/Paste only when in an editable field
                bool showCutPaste = isEditable && !string.IsNullOrWhiteSpace(text);
                popup.ShowCopied(cursorPoint.X, cursorPoint.Y, showPaste: showCutPaste, actions, selectedText: text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in hotkey handler");
            }
            finally
            {
                _processing = false;
            }
        }, DispatcherPriority.Normal);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out CURSORPOINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct CURSORPOINT
    {
        public int X;
        public int Y;
    }

    // ── Process identification helpers ──────────────────────────────────

    /// <summary>
    /// Returns true if the given window handle belongs to this WinClip process.
    /// </summary>
    private static bool IsOwnWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        try
        {
            GetWindowThreadProcessId(hwnd, out uint pid);
            return pid == (uint)Environment.ProcessId;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns true if the foreground window's process name is in the exclusion list.
    /// </summary>
    private bool IsExcludedProcess(IntPtr hwnd)
    {
        if (_settings == null || hwnd == IntPtr.Zero) return false;
        try
        {
            GetWindowThreadProcessId(hwnd, out uint pid);
            using var proc = Process.GetProcessById((int)pid);
            var name = proc.ProcessName.ToLowerInvariant();
            var excluded = _settings.ExcludedProcesses;
            return excluded.Contains(name);
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // ── UI helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new PopupWindow using the user's configured theme.
    /// </summary>
    private PopupWindow CreatePopup()
    {
        var themeKind = _settings?.Theme ?? PopupThemeKind.System;
        var popup = new PopupWindow(themeKind)
        {
            ClipboardHistorySource = _clipboardHistory,
            PinnedActionIds = _settings?.GetPinnedActionIds() ?? Array.Empty<string>(),
            ActionInvocationCounts = _settings?.GetActionInvocationCounts()
                ?? new Dictionary<string, int>()
        };
        popup.PinToggleRequested += OnPinToggleRequested;
        return popup;
    }

    private void OnPinToggleRequested(string actionName)
    {
        if (_settings == null) return;
        var nowPinned = _settings.TogglePinnedAction(actionName);
        _trayIcon?.ShowNotification(
            "WinClip",
            nowPinned ? $"Pinned “{actionName}” to popup row." : $"Unpinned “{actionName}”.",
            System.Windows.Forms.ToolTipIcon.Info);
    }

    private void OnPopupDismissed()
    {
        _currentPopup = null;
    }

    private void DismissPopup()
    {
        _actionCts?.Cancel();
        _actionCts = null;
        if (_currentPopup == null) return;
        try { _currentPopup.DismissImmediately(); }
        catch { /* Window already closed — safe to ignore */ }
        _currentPopup = null;
    }

    private void PlaySound()
    {
        if (!SoundEnabled) return;
        PopSound.Play();
    }

    /// <summary>
    /// Returns all registered actions filtered by the user's enabled actions list.
    /// </summary>
    private IReadOnlyCollection<IAction>? GetEnabledActions()
    {
        var all = _actionRegistry?.GetAllActions();
        if (all == null) return null;
        var enabled = _settings?.EnabledActions;
        if (enabled == null || enabled.Count == 0) return all;

        var enabledSet = new HashSet<string>(enabled);
        return all.Where(a => enabledSet.Contains(a.Name)).ToList().AsReadOnly();
    }

    // ── Selection capture ───────────────────────────────────────────────

    // Clicked buttons can wait longer than a gesture: about 400 ms in all.
    private static readonly IReadOnlyList<int> ButtonCopyDelaysMs = [8, 15, 30, 50, 100, 200];

    private const int TerminalSettleMs = 40;

    /// <summary>
    /// Gets the selected text by UI Automation or a simulated Ctrl+C/Ctrl+X, as
    /// <see cref="SelectionCapturePolicy"/> decides. Terminals never get a keystroke.
    /// Returns null when there is no selection or the capture was skipped.
    /// </summary>
    private async Task<string?> CaptureSelectionAsync(CapturePurpose purpose, IReadOnlyList<int> delaysMs)
    {
        var fg = NativeMethods.GetForegroundWindow();
        bool isTerminal = TerminalWindows.IsTerminal(fg)
            || SelectionPolicy.Default.Evaluate(new PolicyContext(WindowSnapshot.ProcessName(fg), WindowSnapshot.ClassName(fg), null))
                .HasFlag(PolicyEffects.Terminal);
        var uia = UiaSelection.Unavailable;
        if (SelectionCapturePolicy.ShouldReadUia(purpose, isTerminal))
        {
            uia = await UiaSelectionReader.ReadAsync(UiaSelectionReader.DefaultBudget);
            // A UIA call can overtake the gesture's own mouse input in the target's queue,
            // so a terminal may not have made its selection yet. Ctrl+C never had this race.
            if (isTerminal && uia.Status != UiaSelectionStatus.Text)
            {
                await Task.Delay(TerminalSettleMs);
                uia = await UiaSelectionReader.ReadAsync(UiaSelectionReader.DefaultBudget);
            }
        }

        switch (SelectionCapturePolicy.Choose(purpose, isTerminal, uia.Status == UiaSelectionStatus.Text))
        {
            case CaptureRoute.UseUiaText:
                if (purpose == CapturePurpose.Copy)
                {
                    try { WinClip.Actions.ClipboardService.Set(uia.Text!); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Clipboard SetText failed (UIA copy)");
                        return null;
                    }
                }
                return uia.Text;

            case CaptureRoute.SendKeystroke:
                return purpose == CapturePurpose.ReadText
                    ? await ReadSelectionByTransientCopyAsync(delaysMs)
                    : await CopySelectionAsync(purpose == CapturePurpose.Cut, delaysMs);

            default:
                _logger.LogDebug("Selection capture skipped ({Purpose}, terminal: {IsTerminal}, UIA: {Status})",
                    purpose, isTerminal, uia.Status);
                return null;
        }
    }

    // Lets the target app finish a multi-step clipboard write before WinClip restores over it.
    private const int RestoreSettleMs = 150;

    private readonly SemaphoreSlim _transientCopyGate = new(1, 1);

    /// <summary>
    /// Ctrl+C only to read the selection: saves the user's clipboard first and puts it back
    /// afterwards, unless the clipboard changed again in the meantime.
    /// </summary>
    private async Task<string?> ReadSelectionByTransientCopyAsync(IReadOnlyList<int> delaysMs)
    {
        // One at a time: a second action clicked mid-copy would otherwise save the first
        // one's transient copy and restore that instead of the user's clipboard.
        await _transientCopyGate.WaitAsync();
        try
        {
            var snapshot = await ClipboardPreserver.SaveAsync();
            uint before = NativeMethods.GetClipboardSequenceNumber();
            var text = await CopySelectionAsync(cut: false, delaysMs);
            uint atRead = NativeMethods.GetClipboardSequenceNumber();

            if (snapshot == null || atRead == before)
                return text;

            await Task.Delay(RestoreSettleMs);
            if (ClipboardPreserver.ShouldRestore(before, atRead, NativeMethods.GetClipboardSequenceNumber()))
            {
                if (!ClipboardPreserver.Restore(snapshot))
                    _logger.LogWarning("Clipboard restore failed after a transient copy");
            }
            else
            {
                _logger.LogDebug("Clipboard changed after the transient copy; not restoring");
            }
            return text;
        }
        finally
        {
            _transientCopyGate.Release();
        }
    }

    /// <summary>
    /// Sends Ctrl+C (or Ctrl+X) and returns the new clipboard text, or null if the
    /// clipboard never changed or holds no text.
    /// </summary>
    private async Task<string?> CopySelectionAsync(bool cut, IReadOnlyList<int> delaysMs)
    {
        uint before = NativeMethods.GetClipboardSequenceNumber();
        uint sent = cut ? SimulateCut() : SimulateCopy();
        if (sent == 0) return null;

        return await ClipboardChangeWaiter.WaitForNewTextAsync(
            before,
            NativeMethods.GetClipboardSequenceNumber,
            TryReadClipboardText,
            Task.Delay,
            delaysMs);
    }

    private string? TryReadClipboardText()
    {
        try { return Clipboard.GetText(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Clipboard GetText failed (polling)");
            return null;
        }
    }

    // ── Keyboard input simulation ───────────────────────────────────────

    private static uint SimulateCopy()
    {
        var inputs = new[]
        {
            CreateKeyInput(NativeMethods.VK_CONTROL, false),
            CreateKeyInput(NativeMethods.VK_C, false),
            CreateKeyInput(NativeMethods.VK_C, true),
            CreateKeyInput(NativeMethods.VK_CONTROL, true),
        };
        return NativeMethods.SendInput((uint)inputs.Length, inputs, InputStructSize);
    }

    private static uint SimulateCut()
    {
        var inputs = new[]
        {
            CreateKeyInput(NativeMethods.VK_CONTROL, false),
            CreateKeyInput(NativeMethods.VK_X, false),
            CreateKeyInput(NativeMethods.VK_X, true),
            CreateKeyInput(NativeMethods.VK_CONTROL, true),
        };
        return NativeMethods.SendInput((uint)inputs.Length, inputs, InputStructSize);
    }

    internal static uint SimulatePaste()
    {
        var inputs = new[]
        {
            CreateKeyInput(NativeMethods.VK_CONTROL, false),
            CreateKeyInput(NativeMethods.VK_V, false),
            CreateKeyInput(NativeMethods.VK_V, true),
            CreateKeyInput(NativeMethods.VK_CONTROL, true),
        };
        return NativeMethods.SendInput((uint)inputs.Length, inputs, InputStructSize);
    }

    private static NativeMethods.INPUT CreateKeyInput(ushort vk, bool keyUp)
    {
        var input = new NativeMethods.INPUT { type = NativeMethods.INPUT_KEYBOARD };
        input.u.ki.wVk = vk;
        input.u.ki.dwFlags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0;
        return input;
    }

    // ── Hotkey parsing ──────────────────────────────────────────────────

    /// <summary>
    /// Parses a hotkey string like "Ctrl+Shift+C" into Win32 modifier flags and virtual key code.
    /// </summary>
    private static bool TryParseHotkey(string hotkey, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        virtualKey = 0;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= 0x0002; break;  // MOD_CONTROL
                case "alt":              modifiers |= 0x0001; break;  // MOD_ALT
                case "shift":            modifiers |= 0x0004; break;  // MOD_SHIFT
                case "win" or "windows": modifiers |= 0x0008; break;  // MOD_WIN
                default: return false;
            }
        }

        // Last part is the key
        var keyStr = parts[^1].ToUpperInvariant();
        if (keyStr.Length == 1 && char.IsLetterOrDigit(keyStr[0]))
        {
            virtualKey = (uint)keyStr[0]; // A-Z, 0-9 map directly to VK codes
        }
        else
        {
            // Named keys
            virtualKey = keyStr switch
            {
                "SPACE"    => 0x20,
                "ENTER"    => 0x0D,
                "TAB"      => 0x09,
                "ESCAPE"   => 0x1B,
                "DELETE"   => 0x2E,
                "INSERT"   => 0x2D,
                "HOME"     => 0x24,
                "END"      => 0x23,
                "PAGEUP"   => 0x21,
                "PAGEDOWN" => 0x22,
                _ when keyStr.StartsWith("F") && int.TryParse(keyStr[1..], out int fNum) && fNum is >= 1 and <= 24
                    => (uint)(0x6F + fNum), // F1=0x70, F2=0x71, etc.
                _ => 0
            };
        }

        return virtualKey != 0;
    }

}
