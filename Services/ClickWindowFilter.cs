namespace WinClip.Services;

/// <summary>
/// PopClip-style check: a gesture during which a window appeared under the pointer, or the
/// window clicked on went away, was a click on transient UI (a menu, dropdown, dialog or
/// drag image), not a text selection.
/// </summary>
public static class ClickWindowFilter
{
    /// <param name="windowsAtDown">Visible top-level windows at mouse-down; empty if unknown.</param>
    /// <param name="rootAtDown">Top-level window under the pointer at mouse-down.</param>
    /// <param name="rootAtUp">Top-level window under the pointer at mouse-up.</param>
    /// <param name="rootAtDownStillVisible">Whether <paramref name="rootAtDown"/> is visible at mouse-up.</param>
    /// <param name="rootAtUpIsOwn">Whether <paramref name="rootAtUp"/> belongs to WinClip itself.</param>
    /// <returns>A reason when the gesture should be ignored, otherwise null.</returns>
    public static string? RejectReason(
        IReadOnlySet<IntPtr> windowsAtDown,
        IntPtr rootAtDown,
        IntPtr rootAtUp,
        bool rootAtDownStillVisible,
        bool rootAtUpIsOwn)
    {
        // No snapshot means nothing to compare; never block on missing data.
        if (windowsAtDown.Count == 0 || rootAtDown == IntPtr.Zero || rootAtUp == IntPtr.Zero)
            return null;

        if (!rootAtDownStillVisible)
            return "the clicked window disappeared";

        if (rootAtUp != rootAtDown && !rootAtUpIsOwn && !windowsAtDown.Contains(rootAtUp))
            return "a window appeared under the pointer";

        return null;
    }
}
