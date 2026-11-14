using System.Drawing;

using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using WinClip.Actions;
using WinClip.Actions.Image;
using WinClip.Core.Services;
using WinClip.Services;

namespace WinClip.UI;

/// <summary>
/// System tray icon providing status visibility, controls, and update checking.
/// Requires a reference to System.Windows.Forms (included in net8.0-windows with UseWindowsForms).
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ISelectionMonitorService _selectionMonitor;
    private readonly IUpdateService? _updateService;
    private readonly SettingsManager? _settings;
    private readonly IActionRegistry? _actionRegistry;
    private readonly SnippetStore? _snippetStore;
    private readonly ILogger? _logger;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _updateItem;
    private readonly ToolStripMenuItem? _themeMenu;
    private readonly Icon? _enabledIcon;
    private readonly Icon? _disabledIcon;
    private SettingsWindow? _settingsWindow;
    private bool _disposed;
    private bool _checkingForUpdate;

    public TrayIcon(
        ISelectionMonitorService selectionMonitor,
        IUpdateService? updateService = null,
        SettingsManager? settings = null,
        ILogger<TrayIcon>? logger = null,
        IActionRegistry? actionRegistry = null,
        SnippetStore? snippetStore = null)
    {
        _selectionMonitor = selectionMonitor;
        _updateService = updateService;
        _settings = settings;
        _actionRegistry = actionRegistry;
        _snippetStore = snippetStore;
        _logger = logger;

        _enabledIcon = IconGenerator.CreateTrayIcon(paused: false);
        _disabledIcon = IconGenerator.CreateTrayIcon(paused: true);

        _notifyIcon = new NotifyIcon
        {
            Text = $"WinClip v{AppInfo.DisplayVersion}",
            Visible = true,
            Icon = _enabledIcon ?? SystemIcons.Application
        };

        _pauseItem = new ToolStripMenuItem("Pause", null, OnPauseClick);
        _updateItem = new ToolStripMenuItem("Check for Updates...", null, OnCheckForUpdatesClick);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(new ToolStripMenuItem($"WinClip v{AppInfo.DisplayVersion}") { Enabled = false });
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_pauseItem);

        // Sound toggle
        if (_settings != null)
        {
            var soundItem = new ToolStripMenuItem("Sound")
            {
                CheckOnClick = true,
                Checked = _settings.SoundEnabled,
            };
            soundItem.CheckedChanged += (_, _) => _settings.SoundEnabled = soundItem.Checked;
            contextMenu.Items.Add(soundItem);
        }

        // Theme submenu
        if (_settings != null)
        {
            _themeMenu = BuildThemeMenu();
            contextMenu.Items.Add(_themeMenu);
        }

        // Snippets submenu — populated lazily on Opening so newly-saved snippets appear without restart.
        // Each snippet item copies its text to the clipboard on click.
        var snippetMenu = new ToolStripMenuItem("Snippets");
        snippetMenu.DropDownOpening += (_, _) => RebuildSnippetMenu(snippetMenu);
        contextMenu.Items.Add(snippetMenu);

        // Image submenu — populated lazily on Opening so each open reflects current clipboard state
        var imageMenu = new ToolStripMenuItem("Image");
        var saveImageItem = new ToolStripMenuItem("Save Clipboard Image…", null, OnSaveImageClick);
        var qrDecodeItem = new ToolStripMenuItem("Decode QR / Barcode", null, OnDecodeBarcodeClick);
        imageMenu.DropDownItems.Add(saveImageItem);
        imageMenu.DropDownItems.Add(qrDecodeItem);
        imageMenu.DropDownOpening += (_, _) =>
        {
            var hasImage = ClipboardImageService.HasClipboardImage();
            saveImageItem.Enabled = hasImage;
            qrDecodeItem.Enabled = hasImage;
        };
        contextMenu.Items.Add(imageMenu);

        // Settings menu item
        if (_settings != null)
        {
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Settings...", null, OnSettingsClick);
        }

        contextMenu.Items.Add(_updateItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, OnExitClick);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => OnPauseClick(null, EventArgs.Empty);
    }

    /// <summary>
    /// Shows a system tray balloon notification.
    /// </summary>
    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(3000, title, message, icon);
    }

    // ── Theme submenu ──────────────────────────────────────────────────

    private ToolStripMenuItem BuildThemeMenu()
    {
        var menu = new ToolStripMenuItem("Theme");
        var currentTheme = _settings!.Theme;

        // v2.1 simplified to System / Light / Dark only. Legacy themes still parse from
        // settings.json (they all resolve to the Dark palette) but aren't user-pickable.
        // Map any legacy-named current theme onto Dark for the checkmark indicator.
        var displayKind = currentTheme switch
        {
            PopupThemeKind.System => PopupThemeKind.System,
            PopupThemeKind.Light => PopupThemeKind.Light,
            _ => PopupThemeKind.Dark,
        };

        foreach (var kind in new[] { PopupThemeKind.System, PopupThemeKind.Light, PopupThemeKind.Dark })
        {
            var item = new ToolStripMenuItem(kind.ToString())
            {
                CheckOnClick = false,
                Checked = kind == displayKind,
                Tag = kind,
            };
            item.Click += OnThemeItemClick;
            menu.DropDownItems.Add(item);
        }

        return menu;
    }

    private void OnThemeItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem clicked || _settings == null || _themeMenu == null)
            return;

        var kind = (PopupThemeKind)clicked.Tag!;
        _settings.Theme = kind;

        // Update check marks
        foreach (ToolStripMenuItem item in _themeMenu.DropDownItems)
            item.Checked = (PopupThemeKind)item.Tag! == kind;

        _logger?.LogInformation("Theme changed to {Theme}", kind);
    }

    /// <summary>
    /// Performs a background update check. Called from App.OnStartup if auto-check is enabled.
    /// </summary>
    public async Task CheckForUpdateInBackgroundAsync()
    {
        if (_updateService == null || _checkingForUpdate) return;
        _checkingForUpdate = true;

        try
        {
            var update = await _updateService.CheckForUpdateAsync();
            if (update != null)
            {
                ShowUpdateAvailableNotification(update);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Background update check failed");
        }
        finally
        {
            _checkingForUpdate = false;
        }
    }

    private async void OnCheckForUpdatesClick(object? sender, EventArgs e)
    {
        if (_updateService == null || _checkingForUpdate) return;
        _checkingForUpdate = true;
        _updateItem.Text = "Checking...";
        _updateItem.Enabled = false;

        try
        {
            var update = await _updateService.CheckForUpdateAsync();
            if (update != null)
            {
                ShowUpdateAvailableNotification(update);
            }
            else
            {
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "WinClip",
                    "You're running the latest version.",
                    ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Manual update check failed");
            _notifyIcon.ShowBalloonTip(
                3000,
                "WinClip",
                "Could not check for updates. Please try again later.",
                ToolTipIcon.Warning);
        }
        finally
        {
            _checkingForUpdate = false;
            _updateItem.Text = "Check for Updates...";
            _updateItem.Enabled = true;
        }
    }

    private void ShowUpdateAvailableNotification(UpdateInfo update)
    {
        // Always remove BOTH possible handlers before adding one, so repeated
        // update checks don't accumulate Click subscriptions on _updateItem.
        // (Observed bug: second check caused the download dialog to open twice.)
        _notifyIcon.BalloonTipClicked -= OnBalloonTipClicked;
        _updateItem.Click -= OnCheckForUpdatesClick;
        _updateItem.Click -= OnDownloadAndInstallClick;

        // Store the update info for the click handler
        _pendingUpdate = update;
        _notifyIcon.BalloonTipClicked += OnBalloonTipClicked;

        _notifyIcon.ShowBalloonTip(
            5000,
            "WinClip Update Available",
            $"Version {update.Version} is available. Click to download and install.",
            ToolTipIcon.Info);

        _updateItem.Text = $"Update to v{update.Version}...";
        _updateItem.Click += OnDownloadAndInstallClick;
    }

    private UpdateInfo? _pendingUpdate;

    private void OnBalloonTipClicked(object? sender, EventArgs e)
    {
        if (_pendingUpdate != null)
            DownloadAndInstallAsync(_pendingUpdate);
    }

    private void OnDownloadAndInstallClick(object? sender, EventArgs e)
    {
        if (_pendingUpdate != null)
            DownloadAndInstallAsync(_pendingUpdate);
    }

    private async void DownloadAndInstallAsync(UpdateInfo update)
    {
        if (_updateService == null) return;

        _updateItem.Text = "Downloading update...";
        _updateItem.Enabled = false;

        try
        {
            var progress = new Progress<double>(pct =>
            {
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    _updateItem.Text = $"Downloading... {pct:P0}";
                });
            });

            var installerPath = await _updateService.DownloadUpdateAsync(update, progress);

            _updateItem.Text = "Installing...";

            var result = System.Windows.MessageBox.Show(
                $"WinClip v{update.Version} has been downloaded.\n\nInstall now? The application will restart.",
                "WinClip Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _updateService.ApplyUpdate(installerPath);
            }
            else
            {
                _updateItem.Text = $"Update to v{update.Version}...";
                _updateItem.Enabled = true;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download update");
            _notifyIcon.ShowBalloonTip(
                3000,
                "WinClip",
                "Failed to download the update. Please try again later.",
                ToolTipIcon.Warning);
            _updateItem.Text = "Check for Updates...";
            _updateItem.Enabled = true;
            _updateItem.Click -= OnDownloadAndInstallClick;
            _updateItem.Click += OnCheckForUpdatesClick;
        }
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        // Dispatch to WPF Dispatcher — WinForms ContextMenuStrip handlers can
        // prevent WPF windows from activating when created synchronously.
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                _settingsWindow.WindowState = WindowState.Normal;
                _settingsWindow.Activate();
                return;
            }

            try
            {
                _settingsWindow = new SettingsWindow(_settings!, _actionRegistry);
                _settingsWindow.Icon = IconGenerator.CreateWindowIcon();
                _settingsWindow.Closed += (_, _) => _settingsWindow = null;
                _settingsWindow.Show();
                _settingsWindow.Activate();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open Settings window");
                _settingsWindow = null;
                System.Windows.MessageBox.Show(
                    $"Failed to open Settings: {ex.Message}\n\n{ex.InnerException?.Message}",
                    "WinClip Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
    }

    private void OnPauseClick(object? sender, EventArgs e)
    {
        if (_selectionMonitor.IsRunning)
        {
            _selectionMonitor.Stop();
            _pauseItem.Text = "Resume";
            _notifyIcon.Text = $"WinClip v{AppInfo.DisplayVersion} — Paused";
            _notifyIcon.Icon = _disabledIcon ?? SystemIcons.Application;
        }
        else
        {
            _selectionMonitor.Start();
            _pauseItem.Text = "Pause";
            _notifyIcon.Text = $"WinClip v{AppInfo.DisplayVersion}";
            _notifyIcon.Icon = _enabledIcon ?? SystemIcons.Application;
        }
    }

    private static void OnExitClick(object? sender, EventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Application.Current.Shutdown();
        });
    }

    private void OnSaveImageClick(object? sender, EventArgs e)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (!ClipboardImageService.HasClipboardImage())
            {
                ShowNotification("WinClip", "No image on the clipboard to save.", ToolTipIcon.Info);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Save Clipboard Image",
                Filter = "PNG Image (*.png)|*.png",
                FileName = ClipboardImageService.SuggestedFilename(),
                AddExtension = true,
                OverwritePrompt = true,
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            var bytes = ClipboardImageService.SaveAsPng(dlg.FileName);
            if (bytes is { } size)
            {
                _logger?.LogInformation("Saved clipboard image to {Path} ({Size} bytes)", dlg.FileName, size);
                ShowNotification("WinClip — Image saved",
                    $"Saved {size / 1024} KB to {Path.GetFileName(dlg.FileName)}.",
                    ToolTipIcon.Info);
            }
            else
            {
                ShowNotification("WinClip", "Could not save the clipboard image.", ToolTipIcon.Warning);
            }
        });
    }

    private void RebuildSnippetMenu(ToolStripMenuItem snippetMenu)
    {
        snippetMenu.DropDownItems.Clear();

        if (_snippetStore == null)
        {
            snippetMenu.DropDownItems.Add(new ToolStripMenuItem("(snippet store unavailable)") { Enabled = false });
            return;
        }

        var snippets = _snippetStore.GetAll();
        if (snippets.Count == 0)
        {
            snippetMenu.DropDownItems.Add(new ToolStripMenuItem("(no snippets saved yet)") { Enabled = false });
            snippetMenu.DropDownItems.Add(new ToolStripMenuItem("Save selection as snippet from the popup → flyout → “Save Snippet” action") { Enabled = false });
            return;
        }

        foreach (var snippet in snippets.Take(20))
        {
            // Truncate name + use full text as ToolTip so long snippets are still inspectable
            var label = snippet.Name.Length > 60 ? snippet.Name[..57] + "..." : snippet.Name;
            var item = new ToolStripMenuItem(label)
            {
                ToolTipText = snippet.Text.Length > 200 ? snippet.Text[..197] + "..." : snippet.Text,
                Tag = snippet.Text,
            };
            item.Click += OnSnippetItemClick;
            snippetMenu.DropDownItems.Add(item);
        }

        if (snippets.Count > 20)
        {
            snippetMenu.DropDownItems.Add(new ToolStripSeparator());
            snippetMenu.DropDownItems.Add(new ToolStripMenuItem($"+{snippets.Count - 20} more in snippets.json") { Enabled = false });
        }

        snippetMenu.DropDownItems.Add(new ToolStripSeparator());
        snippetMenu.DropDownItems.Add(new ToolStripMenuItem("Open snippets.json folder", null, (_, _) =>
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinClip");
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
            catch (Exception ex) { _logger?.LogWarning(ex, "Could not open snippets folder"); }
        }));
    }

    private void OnSnippetItemClick(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item || item.Tag is not string text) return;
        try
        {
            WinClip.Actions.ClipboardService.Set(text);
            var preview = text.Length > 60 ? text[..57] + "..." : text;
            ShowNotification("Snippet copied", preview, ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Snippet copy failed");
            ShowNotification("WinClip", "Could not copy snippet to clipboard.", ToolTipIcon.Warning);
        }
    }

    private void OnDecodeBarcodeClick(object? sender, EventArgs e)
    {
        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (!ClipboardImageService.HasClipboardImage())
            {
                ShowNotification("WinClip", "No image on the clipboard to decode.", ToolTipIcon.Info);
                return;
            }

            var decoded = ClipboardImageService.DecodeBarcode();
            if (string.IsNullOrEmpty(decoded))
            {
                ShowNotification("WinClip", "No QR or barcode found in the clipboard image.",
                    ToolTipIcon.Info);
                return;
            }

            try
            {
                System.Windows.Clipboard.SetText(decoded);
                var preview = decoded.Length > 60 ? decoded[..57] + "..." : decoded;
                ShowNotification("WinClip — Barcode decoded",
                    $"Copied to clipboard: {preview}", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to copy decoded barcode to clipboard");
                ShowNotification("WinClip", "Decoded the barcode but could not copy it.",
                    ToolTipIcon.Warning);
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
