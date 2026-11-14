using WinClip.Models;
using WinClip.Native;
using WinClip.Services;

namespace WinClip.Tests.Services;

public class ClickWindowFilterTests
{
    private static readonly IntPtr App = new(100);
    private static readonly IntPtr Menu = new(200);
    private static readonly IntPtr Other = new(300);

    private static string? Reject(
        IntPtr down, IntPtr up, bool downVisible = true, bool upIsOwn = false, IntPtr[]? atDown = null)
        => ClickWindowFilter.RejectReason(
            new HashSet<IntPtr>(atDown ?? new[] { App, Other }),
            down, up, downVisible, upIsOwn);

    [Fact]
    public void SameWindowThroughout_IsAllowed()
    {
        Assert.Null(Reject(App, App));
    }

    [Fact]
    public void DragIntoAnotherExistingWindow_IsAllowed()
    {
        Assert.Null(Reject(App, Other));
    }

    [Fact]
    public void WindowAppearedUnderPointer_IsRejected()
    {
        // A dropdown or menu opened by the first click of a double-click.
        Assert.NotNull(Reject(App, Menu));
    }

    [Fact]
    public void ClickedWindowDisappeared_IsRejected()
    {
        // The second click picked a dropdown item and the dropdown closed.
        Assert.NotNull(Reject(Menu, App, downVisible: false, atDown: new[] { App, Menu }));
    }

    [Fact]
    public void OwnWindowAppearing_IsIgnored()
    {
        Assert.Null(Reject(App, Menu, upIsOwn: true));
    }

    [Fact]
    public void MissingSnapshot_NeverBlocks()
    {
        Assert.Null(ClickWindowFilter.RejectReason(new HashSet<IntPtr>(), App, Menu, false, false));
        Assert.Null(Reject(IntPtr.Zero, Menu));
        Assert.Null(Reject(App, IntPtr.Zero));
    }

    [Fact]
    public void Gate_RejectsWindowChange_OnlyWhenHeuristicsAreOn()
    {
        var e = new SelectionEvent(0, 0, SelectionGesture.DoubleClick) { WindowChangeReason = "a window appeared" };
        Assert.NotNull(SelectionGate.RejectReason(e, requireTextCursor: true));
        Assert.Null(SelectionGate.RejectReason(e, requireTextCursor: false));
    }

    [Fact]
    public void LiveSnapshot_DoesNotThrow()
    {
        _ = WindowSnapshot.VisibleTopLevelWindows();
        _ = WindowSnapshot.RootWindowAt(0, 0);
    }
}
