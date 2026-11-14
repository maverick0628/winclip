using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WinClip.Actions;
using WinClip.Native;
using WinClip.Services;
using TextBox = System.Windows.Controls.TextBox;

namespace WinClip.UI;

public partial class PopupWindow : Window
{
    private static readonly TimeSpan FadeInDuration = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan HoverTransitionDuration = TimeSpan.FromMilliseconds(180);
    private static readonly TimeSpan PressScaleDuration = TimeSpan.FromMilliseconds(80);

    // ── Dismiss timing constants ──────────────────────────────────────
    private static readonly TimeSpan DismissAfterPasted = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan DismissAfterCopiedNoActions = TimeSpan.FromSeconds(0.8);
    private static readonly TimeSpan DismissAfterMouseLeave = TimeSpan.FromSeconds(0.4);
    private static readonly TimeSpan DismissAfterFlyoutClose = TimeSpan.FromSeconds(1.0);
    private static readonly TimeSpan DismissAfterCopiedDeferred = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan DismissAfterCopiedWithPaste = TimeSpan.FromSeconds(1.75);
    private static readonly TimeSpan DismissAfterSelection = TimeSpan.FromSeconds(2.0);
    private static readonly TimeSpan DismissAfterActionResult = TimeSpan.FromSeconds(2.0);
    private static readonly TimeSpan DismissAfterCopyClick = TimeSpan.FromSeconds(0.4);
    private static readonly TimeSpan DismissAfterCopyClickWithActions = TimeSpan.FromSeconds(1.75);

    private readonly DispatcherTimer _dismissTimer;
    private bool _isMouseOver;
    private bool _isDismissing;
    private bool _actionFired;

    private IReadOnlyCollection<IAction>? _pendingActions;
    private IReadOnlyCollection<IAction>? _deferredActions;
    private bool _actionsDeferred;
    private bool _flyoutOpen;
    private bool _actionProcessing;
    private string? _selectedText;
    private DispatcherTimer? _flyoutDismissTimer;
    private DispatcherTimer? _processingWatchdog;

    // ── Theme state ──────────────────────────────────────────────────
    private PopupThemeKind _themeKind;
    private PopupThemeColors _colors = null!;

    private SolidColorBrush _textBrush = null!;
    private SolidColorBrush _textHoverBrush = null!;
    private SolidColorBrush _sepBrush = null!;
    private SolidColorBrush _buttonBgBrush = null!;
    private SolidColorBrush _buttonHoverBrush = null!;
    private SolidColorBrush _accentBrush = null!;
    private SolidColorBrush _successBrush = null!;
    private SolidColorBrush _failureBrush = null!;
    private SolidColorBrush _iconBrush = null!;

    public event Action? PasteRequested;
    public event Action? CopyRequested;
    public event Action? CutRequested;
    public event Action<IAction>? ActionRequested;
    public event Action? Dismissed;

    /// <summary>Fired when the user right-clicks a flyout action to toggle its pin state.</summary>
    public event Action<string>? PinToggleRequested;

    /// <summary>
    /// Set this to enable the clipboard history drilldown in the flyout.
    /// When set, clicking "Clipboard History" shows last 10 entries instead of saving.
    /// </summary>
    public ClipboardHistory? ClipboardHistorySource { get; set; }
    internal Func<string, bool> CopyHistoryText { get; set; } = HistoryClipboard.TryCopy;

    /// <summary>
    /// Names of actions currently pinned to the popup top row. Set by the host before showing.
    /// Order matches display order.
    /// </summary>
    public IReadOnlyList<string> PinnedActionIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Local invocation counts per action name. Used to sort flyout entries by frequency
    /// once any action passes <see cref="UserSettings.FlyoutUsageSortThreshold"/>.
    /// </summary>
    public IReadOnlyDictionary<string, int> ActionInvocationCounts { get; set; }
        = new Dictionary<string, int>();

    public PopupWindow() : this(PopupThemeKind.System) { }

    public PopupWindow(PopupThemeKind themeKind)
    {
        InitializeComponent();
        _themeKind = themeKind;
        _colors = PopupTheme.GetColors(_themeKind);
        InitBrushes();
        ApplyTheme();

        _dismissTimer = new DispatcherTimer();
        _dismissTimer.Tick += (_, _) =>
        {
            _dismissTimer.Stop();
            if (!_isMouseOver && !_isDismissing)
                FadeOutAndClose();
        };

        RootBorder.MouseEnter += (_, _) =>
        {
            if (_isDismissing) return;
            _isMouseOver = true;
            _dismissTimer.Stop();

            if (_actionsDeferred && _deferredActions != null)
            {
                _actionsDeferred = false;
                Separator.Visibility = Visibility.Visible;
                PopulateActions(_deferredActions);
                _deferredActions = null;
            }
        };
        RootBorder.MouseLeave += (_, _) =>
        {
            _isMouseOver = false;
            if (!_isDismissing && !_flyoutOpen)
            {
                _dismissTimer.Interval = DismissAfterMouseLeave;
                _dismissTimer.Start();
            }
        };
    }

    // ── Theme application ─────────────────────────────────────────────

    private void InitBrushes()
    {
        _textBrush = Brush(_colors.Text);
        _textHoverBrush = Brush(_colors.TextHover);
        _sepBrush = Brush(_colors.Separator);
        _buttonBgBrush = Brush(_colors.ButtonBackground);
        _buttonHoverBrush = Brush(_colors.ButtonHover);
        _accentBrush = Brush(_colors.AccentColor);
        _successBrush = Brush(_colors.SuccessColor);
        _failureBrush = Brush(_colors.FailureColor);
        _iconBrush = Brush(_colors.IconColor);
    }

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private void ApplyTheme()
    {
        // Gradient background
        GradientStart.Color = (Color)ColorConverter.ConvertFromString(_colors.Background);
        GradientEnd.Color = (Color)ColorConverter.ConvertFromString(_colors.BackgroundGradientEnd);

        // Border — clean stroke, no shadow
        RootBorderBrush.Color = (Color)ColorConverter.ConvertFromString(_colors.Border);
        RootBorder.BorderThickness = new Thickness(1);
        RootBorder.CornerRadius = new CornerRadius(_colors.CornerRadius);

        // Text elements
        StatusText.Foreground = _textBrush;
        StatusIcon.Foreground = _iconBrush;
        SeparatorBrush.Color = (Color)ColorConverter.ConvertFromString(_colors.Separator);
        Separator2Brush.Color = (Color)ColorConverter.ConvertFromString(_colors.Separator);

        // Buttons — filled pill style
        ApplyButtonTheme(CopyButton);
        ApplyButtonTheme(CutButton);
        ApplyButtonTheme(PasteButton);

        ResultText.Foreground = _textBrush;
    }

    private void ApplyButtonTheme(Button btn)
    {
        btn.Background = _buttonBgBrush;
        btn.Foreground = _textBrush;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            exStyle | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW);
    }

    // ── Public show methods ───────────────────────────────────────────

    public void ShowCopied(int screenX, int screenY, bool showPaste = true, IReadOnlyCollection<IAction>? actions = null, string? selectedText = null)
    {
        _dismissTimer.Stop();
        _selectedText = selectedText;

        StatusIcon.Text = "\u2713";
        StatusIcon.Foreground = _iconBrush;
        StatusText.Text = "Copied";
        StatusText.Foreground = _textBrush;

        CopyButton.Visibility = Visibility.Collapsed;

        if (showPaste)
        {
            Separator.Visibility = Visibility.Visible;
            CutButton.Visibility = Visibility.Visible;
            CutButton.Background = _buttonBgBrush;
            SetupButtonHoverEffects(CutButton);
            Separator2.Visibility = Visibility.Visible;
            PasteButton.Visibility = Visibility.Visible;
            PasteButton.Background = _buttonBgBrush;
            SetupButtonHoverEffects(PasteButton);
        }
        else
        {
            CutButton.Visibility = Visibility.Collapsed;
            Separator2.Visibility = Visibility.Collapsed;
            Separator.Visibility = Visibility.Collapsed;
            PasteButton.Visibility = Visibility.Collapsed;
        }

        ResultText.Visibility = Visibility.Collapsed;
        ResultText.Opacity = 0;

        if (!showPaste && actions != null && actions.Count > 0)
        {
            _deferredActions = actions;
            _actionsDeferred = true;
        }
        else
        {
            PopulateActions(actions);
        }

        ShowAtPosition(screenX, screenY);
        _dismissTimer.Interval = showPaste ? DismissAfterCopiedWithPaste : (_actionsDeferred ? DismissAfterCopiedDeferred : DismissAfterCopiedNoActions);
        _dismissTimer.Start();
    }

    public void ShowSelection(int screenX, int screenY, bool clipboardHasText, IReadOnlyCollection<IAction>? actions = null, bool isEditable = true)
    {
        _dismissTimer.Stop();
        _pendingActions = actions;

        StatusIcon.Visibility = Visibility.Collapsed;
        StatusText.Visibility = Visibility.Collapsed;
        Separator.Visibility = Visibility.Collapsed;

        // Copy is always available (works on any selected text)
        CopyButton.Visibility = Visibility.Visible;
        CopyButton.Background = _buttonBgBrush;
        SetupButtonHoverEffects(CopyButton);

        // Cut only makes sense in editable fields (text boxes, editors, etc.)
        if (isEditable)
        {
            CutButton.Visibility = Visibility.Visible;
            CutButton.Background = _buttonBgBrush;
            SetupButtonHoverEffects(CutButton);
        }
        else
        {
            CutButton.Visibility = Visibility.Collapsed;
        }

        // Paste only when clipboard has text AND we're in an editable field
        if (clipboardHasText && isEditable)
        {
            Separator2.Visibility = Visibility.Visible;
            PasteButton.Visibility = Visibility.Visible;
            PasteButton.Background = _buttonBgBrush;
            SetupButtonHoverEffects(PasteButton);
        }
        else
        {
            Separator2.Visibility = Visibility.Collapsed;
            PasteButton.Visibility = Visibility.Collapsed;
        }

        ResultText.Visibility = Visibility.Collapsed;
        ResultText.Opacity = 0;

        PopulateActions(actions);

        ShowAtPosition(screenX, screenY);
        _dismissTimer.Interval = DismissAfterSelection;
        _dismissTimer.Start();
    }

    public void ShowPasted(int screenX, int screenY)
    {
        StatusIcon.Text = "\u2193";
        StatusIcon.Foreground = _iconBrush;
        StatusText.Text = "Pasted";
        StatusText.Foreground = _textBrush;
        Separator.Visibility = Visibility.Collapsed;
        CopyButton.Visibility = Visibility.Collapsed;
        CutButton.Visibility = Visibility.Collapsed;
        Separator2.Visibility = Visibility.Collapsed;
        PasteButton.Visibility = Visibility.Collapsed;
        ActionPanel.Children.Clear();
        ResultText.Visibility = Visibility.Collapsed;
        ResultText.Opacity = 0;
        ShowAtPosition(screenX, screenY);
        _dismissTimer.Interval = DismissAfterPasted;
        _dismissTimer.Start();
    }

    /// <summary>
    /// Guards against the popup getting stuck in Processing state if the action
    /// handler crashes or never returns (which orphaned the pill in the user's
    /// 21:23 attempt). After 35s, force-transition to a "stuck" state and dismiss.
    /// </summary>
    private void StartProcessingWatchdog()
    {
        _processingWatchdog?.Stop();
        _processingWatchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(35) };
        _processingWatchdog.Tick += (_, _) =>
        {
            _processingWatchdog?.Stop();
            if (_actionProcessing && !_isDismissing)
                ShowActionResult(false, "Action timed out — try again");
        };
        _processingWatchdog.Start();
    }

    public void ShowActionResult(bool success, string message)
    {
        _processingWatchdog?.Stop();
        _actionProcessing = false;
        if (_isDismissing) return;
        _dismissTimer.Stop();

        var fadeOutButtons = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(80))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOutButtons.Completed += (_, _) =>
        {
            Separator.Visibility = Visibility.Collapsed;
            CopyButton.Visibility = Visibility.Collapsed;
            CutButton.Visibility = Visibility.Collapsed;
            Separator2.Visibility = Visibility.Collapsed;
            PasteButton.Visibility = Visibility.Collapsed;
            ActionPanel.Children.Clear();

            StatusIcon.Text = success ? "\u2713" : "\u2717";
            StatusIcon.Foreground = success ? _successBrush : _failureBrush;

            StatusText.Text = message;
            StatusText.Foreground = success ? _successBrush : _failureBrush;

            var iconFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            StatusIcon.BeginAnimation(OpacityProperty, iconFade);

            _dismissTimer.Interval = DismissAfterActionResult;
            _dismissTimer.Start();
        };

        PasteButton.BeginAnimation(OpacityProperty, fadeOutButtons);
        ActionPanel.BeginAnimation(OpacityProperty, fadeOutButtons);
    }

    public void DismissImmediately()
    {
        _dismissTimer.Stop();
        _flyoutDismissTimer?.Stop();
        _flyoutDismissTimer = null;
        _actionProcessing = false;
        _isDismissing = true;

        try
        {
            RootBorder.BeginAnimation(OpacityProperty, null);
            Close();
        }
        catch { /* Window already closed */ }

        Dismissed?.Invoke();
        ClearEventHandlers();
    }

    public void ClearEventHandlers()
    {
        PasteRequested = null;
        CopyRequested = null;
        CutRequested = null;
        ActionRequested = null;
        Dismissed = null;
    }

    // ── Action population ─────────────────────────────────────────────

    private void PopulateActions(IReadOnlyCollection<IAction>? actions)
    {
        ActionPanel.Children.Clear();
        if (actions == null || actions.Count == 0) return;

        var filteredActions = actions
            .Where(a => a.Name != "Copy")
            .Where(a => a.IsAvailable())
            .Where(a => a is not IContextualAction ctx
                        || _selectedText == null
                        || ctx.IsRelevantFor(_selectedText))
            .ToList();

        if (filteredActions.Count == 0) return;

        AddActionSeparator();
        AddPinnedActionButtons(filteredActions);
        AddExpandArrowButton(filteredActions);
    }

    /// <summary>
    /// Renders icon-only buttons for the user's pinned actions inline in the popup row,
    /// before the "more ▸" expand arrow. Pinned actions still appear in the flyout list.
    /// </summary>
    private void AddPinnedActionButtons(List<IAction> filteredActions)
    {
        if (PinnedActionIds.Count == 0) return;

        var byName = filteredActions.ToDictionary(a => a.Name, a => a);
        foreach (var pinnedName in PinnedActionIds)
        {
            if (!byName.TryGetValue(pinnedName, out var action)) continue; // not relevant for current text

            var icon = ActionIcons.GetValueOrDefault(action.IconName, "•");
            var btn = new Button
            {
                Content = new TextBlock
                {
                    Text = icon,
                    FontSize = 14,
                    TextAlignment = TextAlignment.Center,
                    Foreground = _accentBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                },
                Style = (Style)FindResource("ActionBtn"),
                Foreground = _accentBrush,
                Background = System.Windows.Media.Brushes.Transparent,
                ToolTip = $"{action.Name} (pinned — right-click to unpin)",
                Padding = new Thickness(8, 5, 8, 5),
                Tag = action,
            };
            SetupButtonHoverEffects(btn);

            var captured = action;
            btn.Click += (_, _) =>
            {
                _actionProcessing = true;
                _dismissTimer.Stop();
                ActionRequested?.Invoke(captured);
            };
            btn.MouseRightButtonUp += (_, e) =>
            {
                e.Handled = true;
                PinToggleRequested?.Invoke(captured.Name);
            };

            ActionPanel.Children.Add(btn);
        }
    }

    private void AddActionSeparator()
    {
        var sep = new Border
        {
            Width = 1,
            Height = 16,
            Background = _sepBrush,
            Margin = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.4,
        };
        ActionPanel.Children.Add(sep);
    }

    // ── Action icon mapping for flyout ──────────────────────────────────
    private static readonly Dictionary<string, string> ActionIcons = new()
    {
        ["Copy"] = "\u2398",
        ["TextTransform"] = "Aa",
        ["Hash"] = "#",
        ["Code"] = "{ }",
        ["Sort"] = "\u2195",
        ["Palette"] = "\u25C9",
        ["Lock"] = "\u26BF",
        ["Email"] = "@",
        ["History"] = "\u29D6",
        ["Table"] = "\u2637",
        ["Dictionary"] = "\u2261",
        ["Globe"] = "\u2609",
        ["Calculator"] = "\u03A3",
        ["Find"] = "\u2315",
        ["QrCode"] = "\u25A3",
        ["Clean"] = "\u2212",
        ["List"] = "\u2116",
        ["Link"] = "\u26D3",
        ["Text"] = "T",
        ["Clock"] = "\u231A",
        ["Translate"] = "\u21C4",
        ["Search"] = "\u2315",
        ["Statistics"] = "\u03C3",
        ["Extract"] = "\u2197",
        ["Snippet"] = "\u2702",
        ["Wrap"] = "\u21A9",
        ["Grammar"] = "\u2713",
        ["Expand"] = "\u2194",
        ["Summarize"] = "\u2026",
        ["Rephrase"] = "\u21BB",
        ["CustomPrompt"] = "\u2605",
        ["Twitter"] = "\u2767",
    };

    private void AddExpandArrowButton(List<IAction> allActions)
    {
        // If any action has crossed the usage threshold, sort the flyout by frequency
        // so power-users find their most-used actions first. Below threshold: keep
        // registration order so the experience is predictable for new users.
        if (ActionInvocationCounts.Count > 0
            && ActionInvocationCounts.Values.Any(c => c >= UserSettings.FlyoutUsageSortThreshold))
        {
            allActions = allActions
                .OrderByDescending(a => ActionInvocationCounts.GetValueOrDefault(a.Name, 0))
                .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var pinnedSet = new HashSet<string>(PinnedActionIds, StringComparer.Ordinal);

        // Ellipsis "more" button — modern and compact
        var expandBtn = new Button
        {
            Content = "\u2026",
            Style = (Style)FindResource("ActionBtn"),
            Foreground = _textBrush,
            Background = System.Windows.Media.Brushes.Transparent,
            ToolTip = $"{allActions.Count} actions",
            Padding = new Thickness(10, 5, 10, 5),
            FontSize = 15,
        };
        SetupButtonHoverEffects(expandBtn);

        expandBtn.Click += (_, _) =>
        {
            _dismissTimer.Stop();
            _flyoutOpen = true;

            var popup = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = expandBtn,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                StaysOpen = true,
                AllowsTransparency = true,
            };

            // Raycast-inspired flyout: search bar with leading icon, accent-tinted
            // icon container per row, footer keyboard-hint bar. Width tuned to 300 —
            // wide enough to read full action names, narrow enough not to dominate.
            const double FlyoutWidth = 300;
            var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };

            // Search bar — borderless, full-width, with leading magnifier icon.
            const int searchThreshold = 6;
            TextBox? searchBox = null;
            if (allActions.Count >= searchThreshold)
            {
                var searchGrid = new Grid { Margin = new Thickness(10, 8, 10, 4) };
                searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var magnifier = new TextBlock
                {
                    Text = "", // Segoe Fluent Icons: search
                    FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                    FontSize = 14,
                    Foreground = _textBrush,
                    Opacity = 0.6,
                    Margin = new Thickness(2, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                Grid.SetColumn(magnifier, 0);
                searchGrid.Children.Add(magnifier);

                searchBox = new TextBox
                {
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = _textHoverBrush,
                    CaretBrush = _accentBrush,
                    FontSize = 14,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI"),
                    Tag = "Find an action…",
                    Padding = new Thickness(0, 6, 0, 6),
                };
                Grid.SetColumn(searchBox, 1);
                searchGrid.Children.Add(searchBox);
                panel.Children.Add(searchGrid);

                // Hairline divider under search
                panel.Children.Add(new Border
                {
                    Height = 1,
                    Background = _sepBrush,
                    Opacity = 0.5,
                    Margin = new Thickness(0, 4, 0, 4),
                });
            }

            var bgColor = (Color)ColorConverter.ConvertFromString(_colors.Background);
            var borderColor = (Color)ColorConverter.ConvertFromString(_colors.Border);

            var border = new Border
            {
                Background = new SolidColorBrush(bgColor),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(0, 6, 0, 0),
                Child = panel,
                Focusable = true,
                Width = FlyoutWidth,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    ShadowDepth = 8,
                    BlurRadius = 24,
                    Color = Color.FromRgb(0, 0, 0),
                    Opacity = 0.35,
                    Direction = 270,
                },
            };

            _flyoutDismissTimer?.Stop();
            _flyoutDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            var flyoutDismissTimer = _flyoutDismissTimer;
            flyoutDismissTimer.Tick += (_, _) =>
            {
                flyoutDismissTimer.Stop();
                if (popup.IsOpen) popup.IsOpen = false;
            };

            border.MouseEnter += (_, _) => flyoutDismissTimer.Stop();
            border.MouseLeave += (_, _) => flyoutDismissTimer.Start();

            // Keyboard navigation state
            int flyoutSelectedIndex = -1;
            var flyoutButtons = new List<Button>();

            // Pre-compute row brushes once. Each row has:
            //   - 32x32 accent-tinted icon container
            //   - 14px title text in TextHover (full-contrast)
            //   - optional pin glyph on the right (Segoe Fluent Icons)
            var accentColor = _accentBrush.Color;
            var iconBgColor = Color.FromArgb(28, accentColor.R, accentColor.G, accentColor.B);

            foreach (var action in allActions)
            {
                var icon = ActionIcons.GetValueOrDefault(action.IconName, "\u2022");

                var rowGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Icon container \u2014 rounded square with accent tint (tighter than 32px)
                var iconBox = new Border
                {
                    Width = 26,
                    Height = 26,
                    CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(iconBgColor),
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = icon,
                        FontSize = 14,
                        FontWeight = FontWeights.Medium,
                        Foreground = _accentBrush,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                Grid.SetColumn(iconBox, 0);
                rowGrid.Children.Add(iconBox);

                // Title
                var titleText = new TextBlock
                {
                    Text = action.Name,
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = _textHoverBrush,
                };
                Grid.SetColumn(titleText, 1);
                rowGrid.Children.Add(titleText);

                var actionBtn = new Button
                {
                    Content = rowGrid,
                    Style = (Style)FindResource("ActionBtn"),
                    Foreground = _textHoverBrush,
                    Background = System.Windows.Media.Brushes.Transparent,
                    ToolTip = action.Description,
                    HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch,
                    Padding = new Thickness(8, 5, 10, 5),
                    Margin = new Thickness(5, 1, 5, 1),
                    Tag = action,
                    Opacity = 1,
                };

                // Pin glyph (Segoe Fluent Icons "Pinned" \ue840) \u2014 appears only if pinned
                if (pinnedSet.Contains(action.Name))
                {
                    var pinGlyph = new TextBlock
                    {
                        Text = "",
                        FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                        FontSize = 13,
                        Foreground = _accentBrush,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(8, 0, 4, 0),
                    };
                    Grid.SetColumn(pinGlyph, 2);
                    rowGrid.Children.Add(pinGlyph);
                }

                // Flyout item hover: fill + lift
                SetupFlyoutItemHover(actionBtn);

                // Right-click on flyout button toggles pin state
                actionBtn.MouseRightButtonUp += (_, e) =>
                {
                    e.Handled = true;
                    PinToggleRequested?.Invoke(action.Name);
                    flyoutDismissTimer.Stop();
                    popup.IsOpen = false;
                };

                var captured = action;
                actionBtn.Click += (_, _) =>
                {
                    // Clipboard History drilldown — show sub-menu with recent entries
                    if (captured.Name == "Clipboard History" && ClipboardHistorySource != null)
                    {
                        flyoutDismissTimer.Stop();
                        ShowClipboardHistoryDrilldown(popup, border, flyoutDismissTimer);
                        return;
                    }

                    flyoutDismissTimer.Stop();
                    popup.IsOpen = false;

                    // Keep popup alive while the action runs (especially async AI calls)
                    _actionProcessing = true;
                    _dismissTimer.Stop();
                    StartProcessingWatchdog();

                    // Show processing state
                    StatusIcon.Visibility = Visibility.Visible;
                    StatusIcon.Text = "\u22EF";
                    StatusIcon.Foreground = _accentBrush;
                    StatusText.Visibility = Visibility.Visible;
                    StatusText.Text = "Processing\u2026";
                    StatusText.Foreground = _textBrush;
                    Separator.Visibility = Visibility.Collapsed;
                    CopyButton.Visibility = Visibility.Collapsed;
                    CutButton.Visibility = Visibility.Collapsed;
                    Separator2.Visibility = Visibility.Collapsed;
                    PasteButton.Visibility = Visibility.Collapsed;
                    ActionPanel.Children.Clear();

                    ActionRequested?.Invoke(captured);
                };
                panel.Children.Add(actionBtn);
                flyoutButtons.Add(actionBtn);
            }

            // Footer hint bar — Raycast-style "↵ Run · → Pin · Esc Close"
            panel.Children.Add(new Border
            {
                Height = 1,
                Background = _sepBrush,
                Opacity = 0.5,
                Margin = new Thickness(0, 4, 0, 0),
            });
            var footer = new TextBlock
            {
                Text = "⏎  Run    →  Pin    Esc  Close",
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI"),
                FontSize = 11,
                Foreground = _textBrush,
                Opacity = 0.7,
                Margin = new Thickness(14, 8, 14, 8),
            };
            panel.Children.Add(footer);

            // ── Type-to-filter wiring ────────────────────────────────
            List<Button> visibleButtons() => flyoutButtons
                .Where(b => b.Visibility == Visibility.Visible).ToList();

            void ApplyFilter(string filter)
            {
                flyoutSelectedIndex = -1;
                foreach (var btn in flyoutButtons)
                {
                    if (btn.Tag is IAction act)
                    {
                        var match = string.IsNullOrWhiteSpace(filter)
                            || act.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
                        btn.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                        btn.Background = System.Windows.Media.Brushes.Transparent;
                        btn.Foreground = _textBrush;
                    }
                }
            }

            if (searchBox != null)
            {
                searchBox.TextChanged += (_, _) => ApplyFilter(searchBox.Text);
                searchBox.PreviewKeyDown += (_, ke) =>
                {
                    // Esc must always close, even when the search filter has zero matches —
                    // checking it before the empty-list early-return avoids a "stuck" flyout.
                    if (ke.Key == Key.Escape)
                    {
                        popup.IsOpen = false;
                        ke.Handled = true;
                        return;
                    }

                    var visible = visibleButtons();
                    if (visible.Count == 0) return;

                    if (ke.Key == Key.Down || ke.Key == Key.Up)
                    {
                        if (flyoutSelectedIndex >= 0 && flyoutSelectedIndex < flyoutButtons.Count)
                        {
                            flyoutButtons[flyoutSelectedIndex].Background = System.Windows.Media.Brushes.Transparent;
                            flyoutButtons[flyoutSelectedIndex].Foreground = _textBrush;
                        }
                        var visIdx = visible.FindIndex(b => b == (flyoutSelectedIndex >= 0 ? flyoutButtons[flyoutSelectedIndex] : null));
                        if (ke.Key == Key.Down)
                            visIdx = visIdx < 0 ? 0 : (visIdx + 1) % visible.Count;
                        else
                            visIdx = visIdx <= 0 ? visible.Count - 1 : visIdx - 1;

                        flyoutSelectedIndex = flyoutButtons.IndexOf(visible[visIdx]);
                        // Accent-tinted "selected" — stronger than hover so keyboard nav
                        // is unambiguous against mouse hover on a different row.
                        var sel = _accentBrush.Color;
                        flyoutButtons[flyoutSelectedIndex].Background =
                            new SolidColorBrush(Color.FromArgb(72, sel.R, sel.G, sel.B));
                        flyoutButtons[flyoutSelectedIndex].Foreground = _textHoverBrush;
                        flyoutDismissTimer.Stop();
                        ke.Handled = true;
                    }
                    else if (ke.Key == Key.Enter)
                    {
                        // If nothing selected yet, activate the first visible match
                        if (flyoutSelectedIndex < 0 && visible.Count > 0)
                            flyoutSelectedIndex = flyoutButtons.IndexOf(visible[0]);

                        if (flyoutSelectedIndex >= 0 && flyoutSelectedIndex < flyoutButtons.Count
                            && flyoutButtons[flyoutSelectedIndex].Tag is IAction selectedAction)
                        {
                            flyoutDismissTimer.Stop();
                            popup.IsOpen = false;
                            ActionRequested?.Invoke(selectedAction);
                            ke.Handled = true;
                        }
                    }
                    else if (ke.Key == Key.Escape)
                    {
                        popup.IsOpen = false;
                        ke.Handled = true;
                    }
                };
            }

            // Keyboard navigation handler (for when search box not present or not focused)
            border.KeyDown += (_, keyArgs) =>
            {
                if (flyoutButtons.Count == 0) return;

                if (keyArgs.Key == Key.Escape)
                {
                    popup.IsOpen = false;
                    keyArgs.Handled = true;
                    return;
                }

                if (keyArgs.Key == Key.Down || keyArgs.Key == Key.Up)
                {
                    var visible = visibleButtons();
                    if (visible.Count == 0) return;

                    // Clear previous highlight
                    if (flyoutSelectedIndex >= 0 && flyoutSelectedIndex < flyoutButtons.Count)
                    {
                        flyoutButtons[flyoutSelectedIndex].Background = System.Windows.Media.Brushes.Transparent;
                        flyoutButtons[flyoutSelectedIndex].Foreground = _textBrush;
                    }

                    var visIdx = visible.FindIndex(b => b == (flyoutSelectedIndex >= 0 ? flyoutButtons[flyoutSelectedIndex] : null));
                    if (keyArgs.Key == Key.Down)
                        visIdx = visIdx < 0 ? 0 : (visIdx + 1) % visible.Count;
                    else
                        visIdx = visIdx <= 0 ? visible.Count - 1 : visIdx - 1;

                    flyoutSelectedIndex = flyoutButtons.IndexOf(visible[visIdx]);
                    var sel2 = _accentBrush.Color;
                    flyoutButtons[flyoutSelectedIndex].Background =
                        new SolidColorBrush(Color.FromArgb(72, sel2.R, sel2.G, sel2.B));
                    flyoutButtons[flyoutSelectedIndex].Foreground = _textHoverBrush;

                    flyoutDismissTimer.Stop();
                    keyArgs.Handled = true;
                }
                else if (keyArgs.Key == Key.Enter && flyoutSelectedIndex >= 0 && flyoutSelectedIndex < flyoutButtons.Count
                         && flyoutButtons[flyoutSelectedIndex].Tag is IAction selectedAction)
                {
                    flyoutDismissTimer.Stop();
                    popup.IsOpen = false;
                    ActionRequested?.Invoke(selectedAction);
                    keyArgs.Handled = true;
                }
            };

            popup.Child = border;

            popup.Closed += (_, _) =>
            {
                _flyoutOpen = false;
                flyoutDismissTimer.Stop();
                // Don't restart dismiss timer if an action is being processed
                if (!_actionProcessing)
                {
                    _dismissTimer.Interval = DismissAfterFlyoutClose;
                    _dismissTimer.Start();
                }
            };

            popup.IsOpen = true;
            flyoutDismissTimer.Start();

            // Focus the search box if present so users can type-to-filter immediately;
            // otherwise focus the border so arrow-key navigation works.
            if (searchBox != null)
            {
                searchBox.Focus();
                Keyboard.Focus(searchBox);
            }
            else
            {
                border.Focus();
                Keyboard.Focus(border);
            }
        };

        ActionPanel.Children.Add(expandBtn);
    }

    // ── Button micro-interactions ─────────────────────────────────────

    private static readonly TimeSpan FlyoutHoverDuration = TimeSpan.FromMilliseconds(80);

    private void SetupFlyoutItemHover(Button btn)
    {
        // Single mutable brush — avoids orphaned animations on rapid hover
        var bgBrush = new SolidColorBrush(Colors.Transparent);
        btn.Background = bgBrush;

        // Accent-tinted hover — Raycast-style. ~20% accent opacity reads as a clear,
        // full-width selection card, not a faint glow. Stronger contrast = better feel.
        var accentColor = _accentBrush.Color;
        var hoverColor = Color.FromArgb(48, accentColor.R, accentColor.G, accentColor.B);

        btn.MouseEnter += (_, _) =>
        {
            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(hoverColor, FlyoutHoverDuration)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
            btn.Foreground = _textHoverBrush;
        };

        btn.MouseLeave += (_, _) =>
        {
            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(Colors.Transparent, FlyoutHoverDuration)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                });
            btn.Foreground = _textBrush;
        };
    }

    // ── Clipboard History drilldown ──────────────────────────────────

    internal void ShowClipboardHistoryDrilldown(
        System.Windows.Controls.Primitives.Popup parentPopup,
        Border parentBorder,
        DispatcherTimer parentDismissTimer)
    {
        var entries = ClipboardHistorySource?.GetAll();
        if (entries == null || entries.Count == 0)
        {
            // No history yet — show a brief message in the parent popup
            var emptyLabel = new TextBlock
            {
                Text = "  No clipboard history yet",
                FontSize = 12,
                Foreground = _sepBrush,
                Margin = new Thickness(10, 6, 10, 6),
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Text, Segoe UI"),
            };
            // Replace parent content temporarily
            if (parentBorder.Child is StackPanel sp)
            {
                sp.Children.Clear();
                sp.Children.Add(emptyLabel);
            }
            return;
        }

        // Build drilldown panel — show last 10 entries
        var drillPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical, MinWidth = 220 };

        // Header with back button
        var header = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(4, 2, 4, 6) };
        var backBtn = new Button
        {
            Content = "\u25C0 Back",
            FontSize = 11,
            Foreground = _accentBrush,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Padding = new Thickness(4, 2, 8, 2),
        };
        header.Children.Add(backBtn);
        header.Children.Add(new TextBlock
        {
            Text = "Clipboard History",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = _textBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
        });
        drillPanel.Children.Add(header);

        var sep = new Border
        {
            Height = 1,
            Background = _sepBrush,
            Opacity = 0.3,
            Margin = new Thickness(4, 0, 4, 4),
        };
        drillPanel.Children.Add(sep);

        var copyError = new TextBlock
        {
            Text = "Clipboard busy — select the clip again to retry.",
            Foreground = _failureBrush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 300,
            Margin = new Thickness(10, 4, 10, 8),
            Visibility = Visibility.Collapsed,
        };
        drillPanel.Children.Add(copyError);

        // Save original content for back button
        var originalChild = parentBorder.Child;

        foreach (var entry in entries.Take(10))
        {
            var preview = entry.Preview;
            var entryBtn = new Button
            {
                Content = preview,
                Style = (Style)FindResource("ActionBtn"),
                Foreground = _textBrush,
                Background = System.Windows.Media.Brushes.Transparent,
                ToolTip = entry.Text.Length > 200 ? entry.Text[..200] + "..." : entry.Text,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                Padding = new Thickness(10, 6, 12, 6),
                FontSize = 12,
                Tag = entry.Text,
                Opacity = 1,
            };

            SetupFlyoutItemHover(entryBtn);

            entryBtn.Click += (_, _) =>
            {
                parentDismissTimer.Stop();
                if (!CopyHistoryText(entry.Text))
                {
                    copyError.Visibility = Visibility.Visible;
                    return;
                }
                parentPopup.IsOpen = false;
                ShowActionResult(true, "Copied from history");
            };
            drillPanel.Children.Add(entryBtn);
        }

        // Back button handler — restore original flyout content
        backBtn.Click += (_, _) =>
        {
            parentBorder.Child = originalChild;
        };

        parentBorder.Child = drillPanel;
    }

    private void SetupButtonHoverEffects(Button btn)
    {
        btn.MouseEnter += (_, _) =>
        {
            if (_isDismissing) return;
            var bgBrush = new SolidColorBrush(_buttonBgBrush.Color);
            btn.Background = bgBrush;
            var colorAnim = new ColorAnimation(_buttonHoverBrush.Color, HoverTransitionDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            bgBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            btn.Foreground = _textHoverBrush;
        };

        btn.MouseLeave += (_, _) =>
        {
            if (_isDismissing) return;
            var bgBrush = btn.Background as SolidColorBrush;
            if (bgBrush != null)
            {
                var colorAnim = new ColorAnimation(_buttonBgBrush.Color, HoverTransitionDuration)
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                bgBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
            }
            btn.Foreground = _textBrush;
        };

        btn.PreviewMouseLeftButtonDown += (_, _) =>
        {
            if (_isDismissing) return;
            var st = EnsureMutableScaleTransform(btn);
            var scaleAnim = new DoubleAnimation(1.0, 0.93, PressScaleDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
        };

        btn.PreviewMouseLeftButtonUp += (_, _) =>
        {
            if (_isDismissing) return;
            var st = EnsureMutableScaleTransform(btn);
            var scaleAnim = new DoubleAnimation(0.93, 1.0, PressScaleDuration)
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
        };
    }

    private static void AnimateButtonPress(Button btn, Action onComplete)
    {
        var st = EnsureMutableScaleTransform(btn);
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        var scaleDown = new DoubleAnimation(1.0, 0.88, TimeSpan.FromMilliseconds(50)) { EasingFunction = ease };
        scaleDown.Completed += (_, _) =>
        {
            var scaleUp = new DoubleAnimation(0.88, 1.0, TimeSpan.FromMilliseconds(70)) { EasingFunction = ease };
            scaleUp.Completed += (_, _) => onComplete();
            var st2 = EnsureMutableScaleTransform(btn);
            st2.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUp);
            st2.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUp);
        };

        st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
    }

    // ── Positioning ───────────────────────────────────────────────────

    private void ShowAtPosition(int screenX, int screenY)
    {
        Left = -9999;
        Top = -9999;
        Opacity = 0;

        Show();

        var monitor = MonitorGeometry.FromPoint(screenX, screenY);
        MaxWidth = Math.Max(1, monitor.WorkArea.Width - 2 * PopupPositioner.Margin);
        MaxHeight = Math.Max(1, monitor.WorkArea.Height - 2 * PopupPositioner.Margin);
        UpdateLayout();

        var (left, top) = PopupPositioner.Calculate(
            screenX, screenY, monitor.ScaleX, monitor.ScaleY,
            ActualWidth > 0 ? ActualWidth : 200,
            ActualHeight > 0 ? ActualHeight : 40,
            monitor.WorkArea);

        Left = left;
        Top = top;

        FadeIn();
    }

    // ── Button click handlers ─────────────────────────────────────────

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try { await HandleCopyOrCut("\u2713", "Copied", () => CopyRequested?.Invoke()); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"CopyButton_Click error: {ex}"); }
    }

    private async void CutButton_Click(object sender, RoutedEventArgs e)
    {
        try { await HandleCopyOrCut("\u2702", "Cut", () => CutRequested?.Invoke()); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"CutButton_Click error: {ex}"); }
    }

    private async Task HandleCopyOrCut(string icon, string label, Action requestAction)
    {
        if (_actionFired) return;
        _actionFired = true;

        StatusIcon.Visibility = Visibility.Visible;
        StatusIcon.Text = icon;
        StatusIcon.Foreground = _iconBrush;
        StatusText.Visibility = Visibility.Visible;
        StatusText.Text = label;
        StatusText.Foreground = _textBrush;
        CopyButton.Visibility = Visibility.Collapsed;
        CutButton.Visibility = Visibility.Collapsed;
        Separator2.Visibility = Visibility.Collapsed;
        PasteButton.Visibility = Visibility.Collapsed;

        requestAction();

        if (_pendingActions != null && _pendingActions.Count > 0)
        {
            await Task.Delay(80);
            try { _selectedText = Clipboard.GetText(); }
            catch { /* Clipboard locked */ }

            Separator.Visibility = Visibility.Visible;
            PopulateActions(_pendingActions);
            _pendingActions = null;
            _actionFired = false;

            _dismissTimer.Stop();
            _dismissTimer.Interval = DismissAfterCopyClickWithActions;
            _dismissTimer.Start();
        }
        else
        {
            Separator.Visibility = Visibility.Collapsed;
            ActionPanel.Children.Clear();

            _dismissTimer.Stop();
            _dismissTimer.Interval = DismissAfterCopyClick;
            _dismissTimer.Start();
        }
    }

    private void PasteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_actionFired) return;
        _actionFired = true;

        _dismissTimer.Stop();
        FadeOutAndClose();

        var pasteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        pasteTimer.Tick += (_, _) =>
        {
            pasteTimer.Stop();
            PasteRequested?.Invoke();
        };
        pasteTimer.Start();
    }

    // ── Animations ────────────────────────────────────────────────────

    private void FadeIn()
    {
        Opacity = 1;
        StatusIcon.Opacity = 1;
        RootBorder.Opacity = 1;
        if (SystemParameters.ClientAreaAnimation)
            RootBorder.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, FadeInDuration));
    }

    private static ScaleTransform EnsureMutableScaleTransform(Button btn)
    {
        if (btn.RenderTransform is ScaleTransform st && !st.IsFrozen)
            return st;

        var fresh = new ScaleTransform(1.0, 1.0);
        btn.RenderTransform = fresh;
        btn.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
        return fresh;
    }

    private void FadeOutAndClose()
    {
        if (_isDismissing) return;
        _isDismissing = true;
        _dismissTimer.Stop();

        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        var opacityAnim = new DoubleAnimation(1, 0, SystemParameters.ClientAreaAnimation ? FadeOutDuration : TimeSpan.Zero) { EasingFunction = ease };
        opacityAnim.Completed += (_, _) =>
        {
            try { Close(); } catch { /* Window already closing */ }
            Dismissed?.Invoke();
        };
        RootBorder.BeginAnimation(OpacityProperty, opacityAnim);


    }
}
