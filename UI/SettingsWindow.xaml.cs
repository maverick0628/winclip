using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using WinClip.Actions;
using WinClip.Core;
using WinClip.Core.Services;
using WinClip.Native;
using WinClip.Services;

namespace WinClip.UI;

public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly SettingsManager _settings;
    private readonly IActionRegistry? _actionRegistry;
    private bool _loading = true; // Suppress events during initial load

    public SettingsWindow(SettingsManager settings, IActionRegistry? actionRegistry = null)
    {
        _settings = settings;
        _actionRegistry = actionRegistry;
        InitializeComponent();
        LoadCurrentSettings();
        _loading = false;

        // Re-apply backdrop dark-mode flag when the user flips Windows light/dark.
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        Closed += (_, _) => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.Color))
            return;
        // FluentWindow's WindowBackdropType="Mica" handles the backdrop redraw automatically;
        // we only need to forward the system theme change to WPF-UI's theme manager.
        Dispatcher.BeginInvoke(() =>
            (System.Windows.Application.Current as App)?.ApplyWpfUiTheme());
    }

    // ── Load ─────────────────────────────────────────────────────────────

    private void LoadCurrentSettings()
    {
        // General
        SoundToggle.IsChecked = _settings.SoundEnabled;
        StartupToggle.IsChecked = _settings.StartWithWindows;
        AutoCopyToggle.IsChecked = _settings.AutoCopyOnDragSelect;
        TextCursorToggle.IsChecked = _settings.RequireTextCursor;
        UsageStatsToggle.IsChecked = _settings.EnableUsageTelemetry;

        // v2.1 reduced theme options to System / Light / Dark. Legacy values
        // (Midnight, Charcoal, Slate, Onyx, Ocean, Forest, Nord, Monokai) still
        // load from settings.json but aren't user-pickable; show as Dark in the
        // dropdown so the user understands their effective theme.
        ThemeCombo.ItemsSource = new[] { PopupThemeKind.System, PopupThemeKind.Light, PopupThemeKind.Dark };
        ThemeCombo.SelectedItem = _settings.Theme switch
        {
            PopupThemeKind.System => PopupThemeKind.System,
            PopupThemeKind.Light => PopupThemeKind.Light,
            _ => PopupThemeKind.Dark,
        };

        RefreshExcludedProcessList();

        // Actions
        ProfileCombo.ItemsSource = Enum.GetValues<ActionProfile>();
        ProfileCombo.SelectedItem = _settings.ActiveProfile;
        PopulateActionCheckboxes();

        // AI
        LoadAISettings();

        // About
        VersionText.Text = $"Version {AppInfo.DisplayVersion}";
    }

    // ── Navigation ───────────────────────────────────────────────────────

    private void SettingsSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SettingsSections == null || SettingsSearchPlaceholder == null) return;
        var query = SettingsSearch.Text.Trim();
        SettingsSearchPlaceholder.Visibility = query.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (FrameworkElement section in SettingsSections.Children)
        {
            var text = string.Join(" ", SearchableText(section));
            section.Visibility = SettingsSearchFilter.Matches(text, query) ? Visibility.Visible : Visibility.Collapsed;
        }
        SettingsScroll.ScrollToTop();
        Dispatcher.BeginInvoke(new Action(UpdateCurrentSection), DispatcherPriority.Loaded);
    }

    private static IEnumerable<string> SearchableText(DependencyObject root)
    {
        if (root is FrameworkElement { Tag: string tag }) yield return tag;
        if (root is TextBlock label) yield return label.Text;
        if (root is ContentControl { Content: string content }) yield return content;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is System.Windows.Controls.TextBox || child is PasswordBox) continue;
            foreach (var text in SearchableText(child)) yield return text;
        }
    }

    private void SettingsScroll_ScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateCurrentSection();

    private void UpdateCurrentSection()
    {
        if (SettingsSections == null || CurrentSection == null) return;
        var visible = SettingsSections.Children.OfType<FrameworkElement>()
            .Where(section => section.Visibility == Visibility.Visible).ToList();
        var current = visible.FirstOrDefault();
        foreach (var section in visible)
        {
            if (section.TranslatePoint(new System.Windows.Point(), SettingsScroll).Y <= 8)
                current = section;
        }
        CurrentSection.Text = current?.Tag?.ToString() ?? "No matching settings — try another search";
    }

    // ── General Tab ──────────────────────────────────────────────────────

    private void SoundToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.SoundEnabled = SoundToggle.IsChecked == true;
    }

    private void StartupToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        bool enabled = StartupToggle.IsChecked == true;
        _settings.StartWithWindows = enabled;
        SetStartWithWindows(enabled);
    }

    private void AutoCopyToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.AutoCopyOnDragSelect = AutoCopyToggle.IsChecked == true;
    }

    private void TextCursorToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.RequireTextCursor = TextCursorToggle.IsChecked == true;
    }

    private void UsageStatsToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.EnableUsageTelemetry = UsageStatsToggle.IsChecked == true;
    }

    private static void SetStartWithWindows(bool enable)
    {
        try
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            const string valueName = "WinClip";

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? "";
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(valueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // May fail in MSIX sandbox — silently ignore
        }
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (ThemeCombo.SelectedItem is PopupThemeKind kind)
        {
            _settings.Theme = kind;
            // Repaint Fluent controls live so the user sees the change without a restart
            (System.Windows.Application.Current as App)?.ApplyWpfUiTheme();
        }
    }

    private DispatcherTimer? _processWarningTimer;

    private void AddExcluded_Click(object sender, RoutedEventArgs e)
    {
        var name = ExcludeProcessInput.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;
        _settings.AddExcludedProcess(name);
        ExcludeProcessInput.Text = "";
        RefreshExcludedProcessList();

        // Validate against running processes
        var normalized = name.Replace(".exe", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();
        bool isRunning;
        try
        {
            isRunning = Process.GetProcesses()
                .Any(p => { try { return p.ProcessName.Equals(normalized, StringComparison.OrdinalIgnoreCase); } catch { return false; } });
        }
        catch { isRunning = false; }

        if (!isRunning)
        {
            ProcessWarning.Text = $"\u26A0 \"{name}\" is not currently running \u2014 check the name.";
            ProcessWarning.Visibility = Visibility.Visible;

            _processWarningTimer?.Stop();
            _processWarningTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _processWarningTimer.Tick += (_, _) =>
            {
                _processWarningTimer.Stop();
                ProcessWarning.Visibility = Visibility.Collapsed;
            };
            _processWarningTimer.Start();
        }
        else
        {
            ProcessWarning.Visibility = Visibility.Collapsed;
        }
    }

    private void RemoveExcluded_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string processName)
        {
            _settings.RemoveExcludedProcess(processName);
            RefreshExcludedProcessList();
        }
    }

    private void RefreshExcludedProcessList()
    {
        ExcludedList.Items.Clear();
        var procs = _settings.ExcludedProcesses ?? new List<string>();
        foreach (var proc in procs)
        {
            var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            panel.Children.Add(new TextBlock
            {
                Text = proc,
                FontSize = 13,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI"),
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 200
            });
            var removeBtn = new Button
            {
                Content = "\u00d7",
                Tag = proc,
                FontSize = 14,
                Padding = new Thickness(6, 2, 6, 2),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = (System.Windows.Media.Brush)FindResource("SurfaceSoftBrush"),
                Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush"),
                BorderThickness = new Thickness(0),
                ToolTip = "Remove",
            };
            removeBtn.Click += RemoveExcluded_Click;
            panel.Children.Add(removeBtn);
            ExcludedList.Items.Add(panel);
        }
    }

    // ── Actions Tab ─────────────────────────────────────────────────────

    private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (ProfileCombo.SelectedItem is not ActionProfile profile) return;

        _settings.ActiveProfile = profile;

        if (profile != ActionProfile.Custom)
        {
            List<string> preset;
            if (profile == ActionProfile.All && _actionRegistry != null)
            {
                preset = _actionRegistry.GetAllActions().Select(a => a.Name).ToList();
            }
            else
            {
                preset = ActionProfilePresets.GetPresetActions(profile).ToList();
            }
            _settings.EnabledActions = preset;

            _loading = true;
            PopulateActionCheckboxes();
            _loading = false;
        }
    }

    private void PopulateActionCheckboxes()
    {
        ActionCheckboxPanel.Children.Clear();
        if (_actionRegistry == null) return;

        var enabledSet = new HashSet<string>(_settings.EnabledActions);
        var allActions = _actionRegistry.GetAllActions()
            .Where(a => a.Name != "Copy") // Copy is always available
            .OrderBy(a => a.Name)
            .ToList();

        foreach (var action in allActions)
        {
            var cb = new System.Windows.Controls.CheckBox
            {
                Content = action.Name,
                IsChecked = enabledSet.Contains(action.Name),
                FontSize = 13,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI"),
                Margin = new Thickness(0, 0, 0, 4),
                ToolTip = action.Description,
                Tag = action.Name,
            };
            cb.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "TextSecondaryBrush");
            cb.Checked += ActionCheckbox_Changed;
            cb.Unchecked += ActionCheckbox_Changed;
            ActionCheckboxPanel.Children.Add(cb);
        }

        UpdateActionCount(enabledSet.Count, allActions.Count + 1); // +1 for Copy
    }

    private void ActionCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;

        var enabled = new List<string> { "Copy" }; // Copy always enabled
        foreach (var child in ActionCheckboxPanel.Children)
        {
            if (child is System.Windows.Controls.CheckBox cb && cb.IsChecked == true && cb.Tag is string name)
                enabled.Add(name);
        }
        _settings.EnabledActions = enabled;

        // Switch to Custom profile on manual toggle
        if (ProfileCombo.SelectedItem is ActionProfile profile && profile != ActionProfile.Custom)
        {
            _loading = true;
            _settings.ActiveProfile = ActionProfile.Custom;
            ProfileCombo.SelectedItem = ActionProfile.Custom;
            _loading = false;
        }

        UpdateActionCount(enabled.Count, ActionCheckboxPanel.Children.Count + 1);
    }

    private void SelectAllActions_Click(object sender, RoutedEventArgs e)
    {
        _loading = true;

        foreach (var child in ActionCheckboxPanel.Children)
        {
            if (child is System.Windows.Controls.CheckBox cb)
                cb.IsChecked = true;
        }

        _loading = false;

        // Collect all and save
        var enabled = new List<string> { "Copy" };
        foreach (var child in ActionCheckboxPanel.Children)
        {
            if (child is System.Windows.Controls.CheckBox cb && cb.Tag is string name)
                enabled.Add(name);
        }
        _settings.EnabledActions = enabled;
        _settings.ActiveProfile = ActionProfile.All;

        _loading = true;
        ProfileCombo.SelectedItem = ActionProfile.All;
        _loading = false;

        UpdateActionCount(enabled.Count, ActionCheckboxPanel.Children.Count + 1);
    }

    private void UnselectAllActions_Click(object sender, RoutedEventArgs e)
    {
        _loading = true;

        foreach (var child in ActionCheckboxPanel.Children)
        {
            if (child is System.Windows.Controls.CheckBox cb)
                cb.IsChecked = false;
        }

        _loading = false;

        // Save — only Copy remains enabled
        var enabled = new List<string> { "Copy" };
        _settings.EnabledActions = enabled;
        _settings.ActiveProfile = ActionProfile.Custom;

        _loading = true;
        ProfileCombo.SelectedItem = ActionProfile.Custom;
        _loading = false;

        UpdateActionCount(enabled.Count, ActionCheckboxPanel.Children.Count + 1);
    }

    private void UpdateActionCount(int enabled, int total)
    {
        ActionCountText.Text = $"{enabled} of {total} actions enabled";
    }

    // ── About Tab ────────────────────────────────────────────────────────

    private void Homepage_Click(object sender, RoutedEventArgs e)
        => OpenUrl("https://duncansmith.tech/apps/winclip/");

    // ── AI Configuration ─────────────────────────────────────────────

    private bool _aiKeyVisible;
    private DispatcherTimer? _aiSaveStatusTimer;

    private void LoadAISettings()
    {
        var provider = _settings.AIProvider ?? "";

        // Select the matching ComboBoxItem by Tag
        foreach (ComboBoxItem item in AIProviderCombo.Items)
        {
            if (item.Tag is string tag && string.Equals(tag, provider, System.StringComparison.OrdinalIgnoreCase))
            {
                AIProviderCombo.SelectedItem = item;
                break;
            }
        }

        // If nothing matched, select "None"
        if (AIProviderCombo.SelectedItem == null)
            AIProviderCombo.SelectedIndex = 0;

        AIApiKeyInput.Text = MaskApiKey(_settings.AIApiKey);
        AIEndpointInput.Text = _settings.AIEndpoint ?? "";
        AIModelInput.Text = _settings.AIModel ?? "";
        _aiKeyVisible = false;

        UpdateAIFieldVisibility(provider);
    }

    private void AIProviderCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (AIProviderCombo.SelectedItem is not ComboBoxItem item) return;
        var provider = item.Tag as string ?? "";

        UpdateAIFieldVisibility(provider);
    }

    private void UpdateAIFieldVisibility(string provider)
    {
        if (AIApiKeyPanel == null) return; // Guard during InitializeComponent

        bool hasProvider = !string.IsNullOrEmpty(provider);
        bool isLmStudio = string.Equals(provider, "lmstudio", System.StringComparison.OrdinalIgnoreCase);

        // LM Studio doesn't require an API key but does require an endpoint
        AIApiKeyPanel.Visibility = hasProvider && !isLmStudio ? Visibility.Visible : Visibility.Collapsed;
        AIEndpointPanel.Visibility = isLmStudio ? Visibility.Visible : Visibility.Collapsed;
        AIModelPanel.Visibility = hasProvider ? Visibility.Visible : Visibility.Collapsed;
        AISavePanel.Visibility = hasProvider ? Visibility.Visible : Visibility.Collapsed;

        if (isLmStudio && string.IsNullOrWhiteSpace(AIEndpointInput.Text))
            AIEndpointInput.Text = "http://localhost:1234";
    }

    private void AIToggleKey_Click(object sender, RoutedEventArgs e)
    {
        _aiKeyVisible = !_aiKeyVisible;
        if (_aiKeyVisible)
        {
            // Show real key
            AIApiKeyInput.Text = _settings.AIApiKey ?? "";
            AIToggleKeyBtn.Content = "Hide";
        }
        else
        {
            // Mask it
            AIApiKeyInput.Text = MaskApiKey(_settings.AIApiKey);
            AIToggleKeyBtn.Content = "Show";
        }
    }

    private void AISave_Click(object sender, RoutedEventArgs e)
    {
        var item = AIProviderCombo.SelectedItem as ComboBoxItem;
        var provider = item?.Tag as string ?? "";

        _settings.AIProvider = string.IsNullOrEmpty(provider) ? null : provider;

        // Only save API key if it was revealed (not masked)
        if (_aiKeyVisible || !AIApiKeyInput.Text.Contains("****"))
            _settings.AIApiKey = string.IsNullOrWhiteSpace(AIApiKeyInput.Text) ? null : AIApiKeyInput.Text.Trim();

        _settings.AIEndpoint = string.IsNullOrWhiteSpace(AIEndpointInput.Text) ? null : AIEndpointInput.Text.Trim();
        _settings.AIModel = string.IsNullOrWhiteSpace(AIModelInput.Text) ? null : AIModelInput.Text.Trim();

        // Sync to appsettings.json so IConfiguration picks it up
        _settings.SyncAISettingsToAppSettings();

        // Show save confirmation
        AISaveStatus.Text = "\u2714 Saved — restart WinClip for AI actions to appear";
        AISaveStatus.Visibility = Visibility.Visible;

        _aiSaveStatusTimer?.Stop();
        _aiSaveStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        _aiSaveStatusTimer.Tick += (_, _) =>
        {
            _aiSaveStatusTimer.Stop();
            AISaveStatus.Visibility = Visibility.Collapsed;
        };
        _aiSaveStatusTimer.Start();

        // Re-mask the key
        if (!_aiKeyVisible && _settings.AIApiKey != null)
        {
            AIApiKeyInput.Text = MaskApiKey(_settings.AIApiKey);
        }
    }

    private static string MaskApiKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        if (key.Length <= 8) return "****";
        return key[..4] + "****" + key[^4..];
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static void OpenUrl(string url)
    {
        try
        {
            if (!SafeUrlLauncher.TryOpenHttpUrl(url))
            {
                System.Windows.MessageBox.Show(
                    "Unable to open link. Only valid http/https URLs are supported.",
                    "Invalid URL",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Unable to open link: {ex.Message}",
                "Open Link Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
