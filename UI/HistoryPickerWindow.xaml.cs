using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using WinClip.Actions;
using WinClip.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Keyboard = System.Windows.Input.Keyboard;
using ModifierKeys = System.Windows.Input.ModifierKeys;
using Key = System.Windows.Input.Key;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace WinClip.UI;

/// <summary>
/// Floating clipboard-history picker. Opens via configurable global hotkey
/// (default Win+Alt+V). Pinned items first, then recency-sorted history;
/// substring search, theme-aware, keyboard-first.
/// </summary>
public partial class HistoryPickerWindow : Window
{
    private readonly ClipboardHistory _history;
    private readonly PinnedHistoryStore _pinned;
    private readonly SettingsManager? _settings;
    private readonly ILogger<HistoryPickerWindow>? _logger;

    private readonly ObservableCollection<HistoryRow> _rows = new();
    private List<HistoryRow> _allRows = new();

    public HistoryPickerWindow(
        ClipboardHistory history,
        PinnedHistoryStore pinned,
        SettingsManager? settings = null,
        ILogger<HistoryPickerWindow>? logger = null)
    {
        InitializeComponent();
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _pinned = pinned ?? throw new ArgumentNullException(nameof(pinned));
        _settings = settings;
        _logger = logger;

        ItemsList.ItemsSource = _rows;
        Loaded += OnLoaded;
        PreviewKeyDown += OnPreviewKeyDown;
        Deactivated += (_, _) => Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
        Reload();
        SearchBox.Focus();
        Keyboard.Focus(SearchBox);
        UpdatePlaceholder();
        PositionOnActiveMonitor();
    }

    /// <summary>Reloads rows from the live <see cref="ClipboardHistory"/> + pin store.</summary>
    public void Reload()
    {
        var pinnedSet = new HashSet<string>(_pinned.GetAll().Select(p => p.Text), StringComparer.Ordinal);
        var pinnedEntries = _pinned.GetAll()
            .Select(p => HistoryRow.Pinned(p.Text, p.PinnedAt));
        var historyEntries = _history.GetAll()
            .Where(h => !pinnedSet.Contains(h.Text))
            .Select(h => HistoryRow.History(h.Text, h.Timestamp));

        _allRows = pinnedEntries.Concat(historyEntries).ToList();
        ApplyFilter(SearchBox.Text);
    }

    private void ApplyFilter(string filter)
    {
        _rows.Clear();
        IEnumerable<HistoryRow> matches = _allRows;

        if (!string.IsNullOrWhiteSpace(filter))
        {
            matches = _allRows.Where(r =>
                r.Text.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var row in matches)
            _rows.Add(row);

        EmptyState.Text = string.IsNullOrWhiteSpace(filter)
            ? "No clipboard history yet. Copy something to get started."
            : "No matching clips. Try another search.";
        EmptyState.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ItemsList.Visibility = _rows.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        if (_rows.Count > 0 && ItemsList.SelectedIndex < 0)
            ItemsList.SelectedIndex = 0;
    }

    // ── Theming ────────────────────────────────────────────────────

    private void ApplyTheme()
    {
        var themeKind = _settings?.Theme ?? PopupThemeKind.System;
        var colors = PopupTheme.GetColors(themeKind);

        var bgStart = (Color)ColorConverter.ConvertFromString(colors.Background);
        var bgEnd = (Color)ColorConverter.ConvertFromString(colors.BackgroundGradientEnd);
        var border = (Color)ColorConverter.ConvertFromString(colors.Border);
        var textHover = (Color)ColorConverter.ConvertFromString(colors.TextHover);
        var text = (Color)ColorConverter.ConvertFromString(colors.Text);
        var btnBg = (Color)ColorConverter.ConvertFromString(colors.ButtonBackground);
        var accent = (Color)ColorConverter.ConvertFromString(colors.AccentColor);

        GradientStart.Color = bgStart;
        GradientEnd.Color = bgEnd;
        RootBorderBrush.Color = border;
        SearchBackgroundBrush.Color = btnBg;
        SearchBorderBrush.Color = border;
        SearchTextBrush.Color = textHover;
        SearchBox.CaretBrush = new SolidColorBrush(textHover);
        Resources["HistoryHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.ButtonHover));
        Resources["HistorySelectionBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors.SelectionBackground));
        Resources["HistoryFocusBrush"] = new SolidColorBrush(accent);
        PlaceholderBrush.Color = text;
        CloseButtonBrush.Color = text;
        FooterBrush.Color = text;
        EmptyStateBrush.Color = text;

        // Push computed colors into rows so DataTemplate bindings resolve
        var rowFg = new SolidColorBrush(textHover); rowFg.Freeze();
        var subtleFg = new SolidColorBrush(text); subtleFg.Freeze();
        var pinFg = new SolidColorBrush(accent); pinFg.Freeze();

        HistoryRow.RowForegroundBrush = rowFg;
        HistoryRow.SubtleForegroundBrush = subtleFg;
        HistoryRow.PinForegroundBrush = pinFg;
    }

    // ── Positioning ────────────────────────────────────────────────

    private void PositionOnActiveMonitor()
    {
        var cursor = System.Windows.Forms.Cursor.Position;
        var monitor = MonitorGeometry.FromPoint(cursor.X, cursor.Y);
        MaxWidth = Math.Max(1, monitor.WorkArea.Width - 16);
        MaxHeight = Math.Max(1, monitor.WorkArea.Height - 16);
        var position = monitor.Center(Math.Min(Width, MaxWidth), Math.Min(Height, MaxHeight));
        Left = position.Left;
        Top = position.Top;
    }

    // ── Keyboard handling ──────────────────────────────────────────

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            var copyOnly = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            ActivateSelected(pasteAfterCopy: !copyOnly);
            return;
        }

        if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            e.Handled = true;
            TogglePinSelected();
            return;
        }

        if (e.Key == Key.Delete && ItemsList.SelectedItem is HistoryRow toDelete)
        {
            e.Handled = true;
            RemoveRow(toDelete);
            return;
        }
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Bounce arrow keys to the list so users can navigate without losing search focus
        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            if (_rows.Count == 0) return;
            int idx = ItemsList.SelectedIndex;
            idx = e.Key == Key.Down ? Math.Min(idx + 1, _rows.Count - 1) : Math.Max(idx - 1, 0);
            ItemsList.SelectedIndex = idx;
            ItemsList.ScrollIntoView(_rows[idx]);
            e.Handled = true;
        }
    }

    private void ItemsList_KeyDown(object sender, KeyEventArgs e)
    {
        // Forward unhandled keys back up to the window-level handler
        if (e.Key == Key.Enter || e.Key == Key.Escape || e.Key == Key.Delete) return;
    }

    private void ItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ActivateSelected(pasteAfterCopy: true);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePlaceholder();
        ApplyFilter(SearchBox.Text);
    }

    private void UpdatePlaceholder()
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    // ── Actions ────────────────────────────────────────────────────

    private void ActivateSelected(bool pasteAfterCopy)
    {
        if (ItemsList.SelectedItem is not HistoryRow row) return;

        if (!HistoryClipboard.TryCopy(row.Text))
        {
            _logger?.LogWarning("Clipboard write failed in history picker");
            FooterHint.Text = "Clipboard busy — try again. Your item is still selected.";
            FooterHint.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                PopupTheme.GetColors(_settings?.Theme ?? PopupThemeKind.System).FailureColor));
            return;
        }

        Close();

        if (pasteAfterCopy)
        {
            // Defer paste briefly so the picker has time to deactivate and the
            // foreground app receives focus before SendInput fires Ctrl+V.
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(80);
                Dispatcher.Invoke(App.SimulatePaste);
            });
        }
    }

    private void TogglePinSelected()
    {
        if (ItemsList.SelectedItem is not HistoryRow row) return;
        if (_pinned.Contains(row.Text))
            _pinned.Remove(row.Text);
        else
            _pinned.Add(row.Text);
        Reload();
    }

    private void RemoveRow(HistoryRow row)
    {
        // For pinned rows: unpin. For history rows: there's no public Remove on
        // ClipboardHistory yet, so we just unpin if applicable. (Future: extend
        // ClipboardHistory with Remove(text) for full delete.)
        if (_pinned.Contains(row.Text))
            _pinned.Remove(row.Text);
        Reload();
    }

    /// <summary>
    /// Maps a hotkey-press event into a window-show on the WPF dispatcher thread.
    /// Reuses the existing instance if visible (toggle behavior).
    /// </summary>
    public static void Toggle(IServiceProvider services)
    {
        var app = Application.Current;
        if (app == null) return;

        app.Dispatcher.BeginInvoke(() =>
        {
            // Close existing instance if visible (toggle off)
            foreach (Window w in app.Windows)
            {
                if (w is HistoryPickerWindow existing)
                {
                    existing.Close();
                    return;
                }
            }

            var history = (ClipboardHistory)services.GetService(typeof(ClipboardHistory))!;
            var pinned = (PinnedHistoryStore)services.GetService(typeof(PinnedHistoryStore))!;
            var settings = services.GetService(typeof(SettingsManager)) as SettingsManager;
            var logger = services.GetService(typeof(ILogger<HistoryPickerWindow>)) as ILogger<HistoryPickerWindow>;

            if (history == null || pinned == null) return;

            var picker = new HistoryPickerWindow(history, pinned, settings, logger);
            picker.Show();
            picker.Activate();
        });
    }
}

/// <summary>
/// View-model row backing each list entry. Brushes are static so the
/// DataTemplate bindings resolve without per-instance allocation.
/// </summary>
public sealed class HistoryRow : INotifyPropertyChanged
{
    public string Text { get; init; } = string.Empty;
    public string Preview { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool IsPinned { get; init; }

    public string PinIcon => IsPinned ? "📌" : " ";
    public string PinTooltip => IsPinned ? "Pinned" : string.Empty;
    public string TimeAgo => FormatRelativeTime(Timestamp);

    public Brush RowForeground => RowForegroundBrush;
    public Brush TimeAgoForeground => SubtleForegroundBrush;
    public Brush PinForeground => IsPinned ? PinForegroundBrush : SubtleForegroundBrush;

    public static Brush RowForegroundBrush { get; set; } = Brushes.White;
    public static Brush SubtleForegroundBrush { get; set; } = Brushes.Gray;
    public static Brush PinForegroundBrush { get; set; } = Brushes.LightSeaGreen;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static HistoryRow Pinned(string text, DateTime pinnedAt) => new()
    {
        Text = text,
        Preview = MakePreview(text),
        Timestamp = pinnedAt,
        IsPinned = true
    };

    public static HistoryRow History(string text, DateTime timestamp) => new()
    {
        Text = text,
        Preview = MakePreview(text),
        Timestamp = timestamp,
        IsPinned = false
    };

    private static string MakePreview(string text) => text.Length <= 80
        ? text.ReplaceLineEndings(" ")
        : text[..77].ReplaceLineEndings(" ") + "...";

    private static string FormatRelativeTime(DateTime utc)
    {
        var delta = DateTime.UtcNow - utc;
        if (delta.TotalSeconds < 60) return "just now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
        if (delta.TotalDays < 7) return $"{(int)delta.TotalDays}d ago";
        return utc.ToLocalTime().ToString("MMM d");
    }
}
