using WinClip.Models;

namespace WinClip.Services;

/// <summary>
/// Decides whether a detected gesture is a real text selection before WinClip sends Ctrl+C.
/// Modelled on PopClip's checks: no Ctrl, Alt or Win held, and a cursor that fits text.
/// </summary>
public static class SelectionGate
{
    private const HeldModifiers Disallowed = HeldModifiers.Control | HeldModifiers.Alt | HeldModifiers.Windows;

    /// <summary>
    /// Returns null when the gesture may proceed, otherwise a short reason for the log.
    /// <paramref name="allowArrowDrag"/> comes from <see cref="SelectionPolicy"/> for apps that
    /// show an arrow while a text drag starts.
    /// </summary>
    public static string? RejectReason(SelectionEvent e, bool requireTextCursor, bool allowArrowDrag = false)
    {
        var held = e.ModifiersAtDown | e.ModifiersAtUp;
        if ((held & Disallowed) != 0)
            return $"modifier held ({held & Disallowed})";

        if (!requireTextCursor)
            return null;

        if (e.WindowChangeReason != null)
            return e.WindowChangeReason;

        // Sampled at mouse-down and mouse-up: a drag that starts on text keeps its I-beam
        // at one end even if the pointer ends over a margin.
        var down = e.CursorAtDown;
        var up = e.CursorAtUp;
        if (down == CursorKind.Unknown || up == CursorKind.Unknown)
            return null;

        return e.Gesture switch
        {
            SelectionGesture.DragSelect when !FitsDrag(down, allowArrowDrag) && !FitsDrag(up, allowArrowDrag)
                => $"drag without a text cursor ({down}, {up})",
            SelectionGesture.DoubleClick when !FitsDoubleClick(down) && !FitsDoubleClick(up)
                => $"double-click without a text cursor ({down}, {up})",
            _ => null,
        };
    }

    private static bool FitsDrag(CursorKind kind, bool allowArrow)
        => kind == CursorKind.IBeam || (allowArrow && kind == CursorKind.Arrow);

    private static bool FitsDoubleClick(CursorKind kind) => kind is CursorKind.IBeam or CursorKind.Arrow;
}
