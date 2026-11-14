using WinClip.Native;
using Xunit;

namespace WinClip.Tests.Native;

public class EditableFieldDetectorTests
{
    [Fact]
    public void HasCaret_DoesNotThrow()
    {
        // HasCaret uses Win32 GetGUIThreadInfo — should never throw,
        // even when no foreground window has a caret (e.g. in test runner)
        bool result = EditableFieldDetector.HasCaret();

        // In a test runner context, no caret is expected
        Assert.False(result);
    }

    [Fact]
    public void IsFocusedControlEditable_ReturnsWithoutThrowing()
    {
        // Should never throw regardless of what's at the given coordinates.
        // Uses UI Automation which may return varying results depending on
        // what's under (0,0) but must not crash.
        bool result = EditableFieldDetector.IsFocusedControlEditable(0, 0);

        // Result depends on what's at screen origin — just verify it returns
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void IsFocusedControlEditable_NegativeCoordinates_DoesNotThrow()
    {
        // Edge case: coordinates outside any screen
        bool result = EditableFieldDetector.IsFocusedControlEditable(-9999, -9999);
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void IsFocusedControlEditable_LargeCoordinates_DoesNotThrow()
    {
        // Edge case: coordinates beyond any reasonable screen
        bool result = EditableFieldDetector.IsFocusedControlEditable(99999, 99999);
        Assert.IsType<bool>(result);
    }
}
