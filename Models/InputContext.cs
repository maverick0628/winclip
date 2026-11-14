namespace WinClip.Models;

/// <summary>
/// Modifier keys held during a gesture, sampled from the physical keyboard state.
/// </summary>
[Flags]
public enum HeldModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Windows = 8,
}

/// <summary>
/// What the mouse cursor looked like when a gesture was sampled.
/// Only system cursors are classified; an app's own cursor is <see cref="Unknown"/>.
/// </summary>
public enum CursorKind
{
    /// <summary>Hidden, unreadable or an app-specific cursor. Never blocks a gesture.</summary>
    Unknown,
    IBeam,
    Arrow,
    Hand,
    /// <summary>Resize, move, busy, crosshair or "not allowed": never a text selection.</summary>
    NonText,
}
