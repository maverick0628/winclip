using System.Runtime.InteropServices;

namespace WinClip.Native;

/// <summary>
/// Constants and structures for Windows hook procedures.
/// </summary>
internal static class HookTypes
{
    /// <summary>
    /// Hook type for monitoring low-level mouse input events.
    /// </summary>
    internal const int WH_MOUSE_LL = 14;

    /// <summary>
    /// Message indicating the left mouse button was released.
    /// </summary>
    internal const int WM_LBUTTONUP = 0x0202;

    /// <summary>
    /// Message indicating the left mouse button was pressed.
    /// </summary>
    internal const int WM_LBUTTONDOWN = 0x0201;

    /// <summary>
    /// Message indicating the right mouse button was released.
    /// </summary>
    internal const int WM_RBUTTONUP = 0x0205;

    /// <summary>
    /// Message indicating the right mouse button was pressed.
    /// </summary>
    internal const int WM_RBUTTONDOWN = 0x0204;

    /// <summary>
    /// Message indicating mouse movement.
    /// </summary>
    internal const int WM_MOUSEMOVE = 0x0200;

    /// <summary>
    /// Hook code indicating the message should be processed.
    /// </summary>
    internal const int HC_ACTION = 0;

    /// <summary>
    /// Hook type for monitoring low-level keyboard input events.
    /// </summary>
    internal const int WH_KEYBOARD_LL = 13;

    internal const int WM_KEYDOWN = 0x0100;
    internal const int WM_KEYUP = 0x0101;
    internal const int WM_SYSKEYDOWN = 0x0104;
    internal const int WM_SYSKEYUP = 0x0105;
}

/// <summary>
/// Contains information about a low-level keyboard input event.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct KBDLLHOOKSTRUCT
{
    public uint vkCode;
    public uint scanCode;
    public uint flags;
    public uint time;
    public IntPtr dwExtraInfo;
}

/// <summary>
/// Contains information about a low-level mouse input event.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct MSLLHOOKSTRUCT
{
    /// <summary>
    /// X and Y coordinates of the cursor, in screen coordinates.
    /// </summary>
    public POINT pt;

    /// <summary>
    /// Mouse data (e.g., wheel delta, X button information).
    /// </summary>
    public uint mouseData;

    /// <summary>
    /// Event-injected flags.
    /// </summary>
    public uint flags;

    /// <summary>
    /// Time stamp for this message.
    /// </summary>
    public uint time;

    /// <summary>
    /// Additional information associated with the message.
    /// </summary>
    public IntPtr dwExtraInfo;
}

/// <summary>
/// Represents a point with X and Y coordinates.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    /// <summary>
    /// X coordinate of the point.
    /// </summary>
    public int x;

    /// <summary>
    /// Y coordinate of the point.
    /// </summary>
    public int y;
}
