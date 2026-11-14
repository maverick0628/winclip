using WinClip.Native;

namespace WinClip.Services;

/// <summary>
/// Detects text-selection gestures from a stream of mouse events:
///   1. Double-click  (two rapid clicks in the same spot → word selection)
///   2. Drag-select   (mouse down → move beyond threshold → mouse up)
///
/// Ignores simple single clicks and tiny drags (accidental jitter).
/// </summary>
public class SelectionDetector
{
    // Double-click tuning
    private readonly int _doubleClickMaxTimeMs;
    private readonly int _doubleClickMaxDistancePx;

    // Drag-select tuning
    private readonly int _dragMinDistancePx;

    // State for double-click detection
    private DateTime _lastClickUpTime = DateTime.MinValue;
    private int _lastClickUpX;
    private int _lastClickUpY;

    // State for drag detection
    private bool _isMouseDown;
    private int _mouseDownX;
    private int _mouseDownY;
    private DateTime _mouseDownTime;

    public SelectionDetector(
        int doubleClickMaxTimeMs = 500,
        int doubleClickMaxDistancePx = 10,
        int dragMinDistancePx = 15)
    {
        _doubleClickMaxTimeMs = doubleClickMaxTimeMs;
        _doubleClickMaxDistancePx = doubleClickMaxDistancePx;
        _dragMinDistancePx = dragMinDistancePx;
    }

    /// <summary>
    /// True when a mouse-down at this point and time would be the second click of a
    /// double-click. Call before <see cref="OnMouseDown"/>.
    /// </summary>
    public bool IsSecondClick(int x, int y, DateTime timestamp)
        => _lastClickUpTime != DateTime.MinValue
        && (timestamp - _lastClickUpTime).TotalMilliseconds < _doubleClickMaxTimeMs
        && Math.Abs(x - _lastClickUpX) < _doubleClickMaxDistancePx
        && Math.Abs(y - _lastClickUpY) < _doubleClickMaxDistancePx;

    /// <summary>
    /// Call when left mouse button goes down.
    /// </summary>
    public void OnMouseDown(int x, int y, DateTime timestamp)
    {
        _isMouseDown = true;
        _mouseDownX = x;
        _mouseDownY = y;
        _mouseDownTime = timestamp;
    }

    /// <summary>
    /// Call when left mouse button goes up.
    /// Returns the type of selection gesture detected (None, DoubleClick, or DragSelect).
    /// </summary>
    public SelectionGesture OnMouseUp(int x, int y, DateTime timestamp)
    {
        var gesture = SelectionGesture.None;

        if (_isMouseDown)
        {
            // Check drag distance from mouse-down to mouse-up
            int dragDx = Math.Abs(x - _mouseDownX);
            int dragDy = Math.Abs(y - _mouseDownY);
            int dragDist = Math.Max(dragDx, dragDy);

            if (dragDist >= _dragMinDistancePx)
            {
                // Significant drag — likely text selection
                gesture = SelectionGesture.DragSelect;
                // Reset double-click state since this was a drag
                _lastClickUpTime = DateTime.MinValue;
            }
            else
            {
                // Small/no drag — check for double-click
                var elapsed = (timestamp - _lastClickUpTime).TotalMilliseconds;
                int dx = Math.Abs(x - _lastClickUpX);
                int dy = Math.Abs(y - _lastClickUpY);

                if (elapsed < _doubleClickMaxTimeMs
                    && dx < _doubleClickMaxDistancePx
                    && dy < _doubleClickMaxDistancePx)
                {
                    gesture = SelectionGesture.DoubleClick;
                    // Reset so third click doesn't re-trigger
                    _lastClickUpTime = DateTime.MinValue;
                }
                else
                {
                    // First click — remember for potential double-click
                    _lastClickUpTime = timestamp;
                    _lastClickUpX = x;
                    _lastClickUpY = y;
                }
            }
        }

        _isMouseDown = false;
        return gesture;
    }
}

public enum SelectionGesture
{
    None,
    DoubleClick,
    DragSelect,
    SingleClick
}
