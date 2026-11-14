using WinClip.Native;
using WinClip.Services;

namespace WinClip.Models;

public sealed class SelectionEvent
{
    public SelectionEvent(int cursorX, int cursorY, SelectionGesture gesture)
    {
        CursorX = cursorX;
        CursorY = cursorY;
        Gesture = gesture;
        Timestamp = DateTime.UtcNow;
    }

    public SelectionEvent(int cursorX, int cursorY, bool isDoubleClick)
        : this(cursorX, cursorY, isDoubleClick ? SelectionGesture.DoubleClick : SelectionGesture.DragSelect)
    {
    }

    public SelectionEvent(POINT cursorPosition, bool isDoubleClick)
        : this(cursorPosition.x, cursorPosition.y, isDoubleClick)
    {
    }

    public DateTime Timestamp { get; }
    public int CursorX { get; }
    public int CursorY { get; }
    public SelectionGesture Gesture { get; }

    public HeldModifiers ModifiersAtDown { get; init; }
    public HeldModifiers ModifiersAtUp { get; init; }
    public CursorKind CursorAtDown { get; init; }
    public CursorKind CursorAtUp { get; init; }

    /// <summary>Set when a window appeared or vanished under the pointer during the gesture.</summary>
    public string? WindowChangeReason { get; init; }

    public bool IsDoubleClick => Gesture == SelectionGesture.DoubleClick;
    public bool IsSingleClick => Gesture == SelectionGesture.SingleClick;
    public bool IsDragSelect => Gesture == SelectionGesture.DragSelect;
}
