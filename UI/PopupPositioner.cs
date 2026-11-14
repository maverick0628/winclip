using System.Windows;

namespace WinClip.UI;

/// <summary>
/// Pure positioning math for the popup toast. Fully testable without WPF visual tree.
/// Positions the popup centered above the cursor, like PopClip on Mac.
/// </summary>
public static class PopupPositioner
{
    public const double GapAboveCursor = 12;
    public const double Margin = 8;

    /// <summary>
    /// Calculates the popup position in WPF device-independent units, clamped to the work area.
    /// Centers the popup horizontally above the cursor position.
    /// </summary>
    public static (double Left, double Top) Calculate(
        int screenX, int screenY,
        double dpiScaleX, double dpiScaleY,
        double popupWidth, double popupHeight,
        Rect workArea)
    {
        // Convert screen pixels to WPF DIPs
        double cursorX = screenX * dpiScaleX;
        double cursorY = screenY * dpiScaleY;

        // Center horizontally above cursor
        double left = cursorX - popupWidth / 2;
        // Position above cursor with a gap
        double top = cursorY - popupHeight - GapAboveCursor;

        // Clamp to work area
        if (left + popupWidth > workArea.Right - Margin)
            left = workArea.Right - popupWidth - Margin;
        if (left < workArea.Left + Margin)
            left = workArea.Left + Margin;

        // If popup would be above work area, flip to below cursor
        if (top < workArea.Top + Margin)
            top = cursorY + GapAboveCursor;

        if (top + popupHeight > workArea.Bottom - Margin)
            top = workArea.Bottom - popupHeight - Margin;

        return (left, top);
    }
}
