using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace WinClip.UI;

/// <summary>
/// Small floating preview window that surfaces during AI-action execution.
/// Shows a spinner + action title + sub-status while the API call is in flight,
/// then morphs to a success or error state and auto-dismisses.
///
/// Designed to be non-blocking: pinned top-most in the corner of the active
/// monitor so the user knows the request is still running and can keep typing.
/// </summary>
public partial class AIPreviewWindow : Window
{
    // Segoe Fluent Icons codepoints
    private const string SpinnerGlyph = ""; // Sync (we rotate it)
    private const string SuccessGlyph = ""; // Checkmark
    private const string ErrorGlyph = "";   // ErrorBadge

    private static AIPreviewWindow? _current;
    private readonly Storyboard _spinStoryboard;
    private readonly DispatcherTimer _autoCloseTimer;
    private bool _closing;

    private AIPreviewWindow()
    {
        InitializeComponent();

        // Continuous 360° rotation for the spinner glyph during Processing state
        var rot = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(1.2)))
        {
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTarget(rot, GlyphSpin);
        Storyboard.SetTargetProperty(rot, new PropertyPath(RotateTransform.AngleProperty));
        _spinStoryboard = new Storyboard();
        _spinStoryboard.Children.Add(rot);

        _autoCloseTimer = new DispatcherTimer();
        _autoCloseTimer.Tick += (_, _) =>
        {
            _autoCloseTimer.Stop();
            DismissImmediate();
        };

        Loaded += (_, _) => FadeIn();
        Deactivated += (_, _) => { /* don't auto-close; this window is informational */ };
    }

    /// <summary>
    /// Show the spinner state for the given action. If a preview is already open,
    /// updates its title in place rather than stacking windows.
    /// </summary>
    public static AIPreviewWindow ShowProcessing(string actionName, PopupThemeKind theme)
    {
        var app = Application.Current;
        if (app == null) throw new InvalidOperationException("No WPF Application context");

        return (AIPreviewWindow)app.Dispatcher.Invoke(() =>
        {
            if (_current is { _closing: false })
            {
                _current.SetProcessing(actionName);
                return _current;
            }

            var w = new AIPreviewWindow();
            w.ApplyTheme(theme);
            w.SetProcessing(actionName);
            _current = w;
            w.PositionNearCursor();
            w.Show();
            return w;
        });
    }

    public void SetProcessing(string actionName)
    {
        Dispatcher.Invoke(() =>
        {
            StatusGlyph.Text = SpinnerGlyph;
            TitleText.Text = $"{actionName}…";
            StatusText.Text = "Working with your AI provider";
            _spinStoryboard.Begin(this, true);
            _autoCloseTimer.Stop();
        });
    }

    public void ShowSuccess(string actionName, string? detail = null)
    {
        Dispatcher.Invoke(() =>
        {
            _spinStoryboard.Stop(this);
            GlyphSpin.Angle = 0;
            StatusGlyph.Text = SuccessGlyph;
            // Keep the accent color for success — feels native to WinClip's teal
            TitleText.Text = $"{actionName} done";
            StatusText.Text = detail ?? "Result copied to clipboard";
            ScheduleAutoClose(TimeSpan.FromSeconds(1.6));
        });
    }

    /// <summary>
    /// Show the AI result text with a manual Copy button — used when clipboard
    /// auto-write was blocked by another app holding the clipboard lock.
    /// Stays on screen until the user clicks Copy or dismisses.
    /// </summary>
    public void ShowResultForManualCopy(string actionName, string resultText)
    {
        Dispatcher.Invoke(() =>
        {
            _spinStoryboard.Stop(this);
            GlyphSpin.Angle = 0;
            StatusGlyph.Text = SuccessGlyph;
            TitleText.Text = $"{actionName} — copy needed";

            // Replace the simple status text with a result preview + Copy button
            var grid = (System.Windows.Controls.Grid)StatusText.Parent;
            // Show the result snippet (truncated for the preview)
            var preview = resultText.Length > 320 ? resultText[..317] + "…" : resultText;
            StatusText.Text = preview;
            StatusText.MaxWidth = 320;
            StatusText.MaxHeight = 100;
            StatusText.TextWrapping = System.Windows.TextWrapping.Wrap;

            // Inject the Copy button if it isn't there yet
            if (CopyResultButton == null)
            {
                CopyResultButton = new System.Windows.Controls.Button
                {
                    Content = "Copy",
                    FontSize = 12,
                    Padding = new System.Windows.Thickness(12, 4, 12, 4),
                    Margin = new System.Windows.Thickness(0, 8, 0, 0),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderBrush = new SolidColorBrush(GlyphBrush.Color),
                    BorderThickness = new System.Windows.Thickness(1),
                    Foreground = new SolidColorBrush(GlyphBrush.Color),
                    Cursor = System.Windows.Input.Cursors.Hand,
                };
                CopyResultButton.Click += (_, _) =>
                {
                    if (Actions.ClipboardService.TrySet(resultText))
                    {
                        StatusText.Text = "Copied to clipboard";
                        CopyResultButton.IsEnabled = false;
                        CopyResultButton.Content = "Copied";
                        ScheduleAutoClose(TimeSpan.FromSeconds(1.5));
                    }
                    else
                    {
                        StatusText.Text = "Clipboard still busy — try again in a moment";
                    }
                };
                ((System.Windows.Controls.StackPanel)StatusText.Parent).Children.Add(CopyResultButton);
            }
            // No auto-close while waiting for the user to click Copy
            _autoCloseTimer.Stop();
        });
    }

    private System.Windows.Controls.Button? CopyResultButton;

    public void ShowError(string actionName, string detail)
    {
        Dispatcher.Invoke(() =>
        {
            _spinStoryboard.Stop(this);
            GlyphSpin.Angle = 0;
            StatusGlyph.Text = ErrorGlyph;
            // Switch glyph + bg to a danger color for errors
            GlyphBrush.Color = (Color)ColorConverter.ConvertFromString("#F87171");
            IconBg.Background = new SolidColorBrush(Color.FromArgb(28, 248, 113, 113));
            TitleText.Text = $"{actionName} failed";
            StatusText.Text = detail.Length > 280 ? detail[..277] + "…" : detail;
            // Errors stay on screen longer so the user can read them
            ScheduleAutoClose(TimeSpan.FromSeconds(6));
        });
    }

    public void DismissImmediate()
    {
        if (_closing) return;
        _closing = true;
        _spinStoryboard.Stop(this);
        var fade = new DoubleAnimation(RootBorder.Opacity, 0, TimeSpan.FromMilliseconds(160))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        fade.Completed += (_, _) =>
        {
            try { Close(); } catch (Exception ex) { Debug.WriteLine($"AIPreviewWindow close failed: {ex.Message}"); }
            if (_current == this) _current = null;
        };
        RootBorder.BeginAnimation(OpacityProperty, fade);
    }

    private void FadeIn()
    {
        RootBorder.Opacity = 1;
        if (SystemParameters.ClientAreaAnimation)
            RootBorder.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(50)));
    }

    private void ScheduleAutoClose(TimeSpan delay)
    {
        _autoCloseTimer.Stop();
        _autoCloseTimer.Interval = delay;
        _autoCloseTimer.Start();
    }

    private void ApplyTheme(PopupThemeKind theme)
    {
        var c = PopupTheme.GetColors(theme);
        RootBg.Color = (Color)ColorConverter.ConvertFromString(c.Background);
        RootBorderBrush.Color = (Color)ColorConverter.ConvertFromString(c.Border);
        TitleBrush.Color = (Color)ColorConverter.ConvertFromString(c.TextHover);
        StatusBrush.Color = (Color)ColorConverter.ConvertFromString(c.Text);
        var accent = (Color)ColorConverter.ConvertFromString(c.AccentColor);
        GlyphBrush.Color = accent;
        IconBg.Background = new SolidColorBrush(Color.FromArgb(28, accent.R, accent.G, accent.B));
    }

    /// <summary>
    /// Anchor near the cursor so the preview lands where the user is already looking
    /// (right below the popup that triggered it). Falls back to keeping the window
    /// fully on-screen when the cursor is near the bottom or right edge.
    /// </summary>
    private void PositionNearCursor()
    {
        var cursor = System.Windows.Forms.Cursor.Position;
        var screen = System.Windows.Forms.Screen.FromPoint(cursor).WorkingArea;

        Width = 320;
        const double ApproxHeight = 70;
        const double Gap = 110; // below the popup, which sits ~80px tall above cursor

        var left = cursor.X - Width / 2;
        var top = cursor.Y + Gap;

        // Clamp inside the working area
        if (left < screen.Left + 12) left = screen.Left + 12;
        if (left + Width > screen.Right - 12) left = screen.Right - Width - 12;
        if (top + ApproxHeight > screen.Bottom - 12) top = cursor.Y - ApproxHeight - 24;
        if (top < screen.Top + 12) top = screen.Top + 12;

        Left = left;
        Top = top;
        Debug.WriteLine($"[AIPreviewWindow] positioned at ({left:0},{top:0}) on screen {screen}");
    }
}
