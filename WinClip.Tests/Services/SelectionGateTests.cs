using WinClip.Models;
using WinClip.Services;

namespace WinClip.Tests.Services;

public class SelectionGateTests
{
    private static SelectionEvent Event(
        SelectionGesture gesture,
        CursorKind down = CursorKind.IBeam,
        CursorKind up = CursorKind.IBeam,
        HeldModifiers modsDown = HeldModifiers.None,
        HeldModifiers modsUp = HeldModifiers.None)
        => new(100, 100, gesture)
        {
            CursorAtDown = down,
            CursorAtUp = up,
            ModifiersAtDown = modsDown,
            ModifiersAtUp = modsUp,
        };

    [Theory]
    [InlineData(SelectionGesture.DoubleClick)]
    [InlineData(SelectionGesture.DragSelect)]
    public void PlainGestureOverText_IsAllowed(SelectionGesture gesture)
    {
        Assert.Null(SelectionGate.RejectReason(Event(gesture), requireTextCursor: true));
    }

    [Theory]
    [InlineData(HeldModifiers.Control)]
    [InlineData(HeldModifiers.Alt)]
    [InlineData(HeldModifiers.Windows)]
    [InlineData(HeldModifiers.Control | HeldModifiers.Shift)]
    public void DisallowedModifierAtMouseDown_IsRejected(HeldModifiers held)
    {
        var e = Event(SelectionGesture.DragSelect, modsDown: held);
        Assert.NotNull(SelectionGate.RejectReason(e, requireTextCursor: true));
    }

    [Fact]
    public void DisallowedModifierAtMouseUpOnly_IsRejected()
    {
        var e = Event(SelectionGesture.DoubleClick, modsUp: HeldModifiers.Control);
        Assert.NotNull(SelectionGate.RejectReason(e, requireTextCursor: true));
    }

    [Fact]
    public void ModifiersAreRejectedEvenWithCursorGateOff()
    {
        var e = Event(SelectionGesture.DragSelect, modsDown: HeldModifiers.Alt);
        Assert.NotNull(SelectionGate.RejectReason(e, requireTextCursor: false));
    }

    [Fact]
    public void ShiftAlone_IsAllowed()
    {
        var e = Event(SelectionGesture.DragSelect, modsDown: HeldModifiers.Shift, modsUp: HeldModifiers.Shift);
        Assert.Null(SelectionGate.RejectReason(e, requireTextCursor: true));
    }

    [Theory]
    [InlineData(CursorKind.Arrow, CursorKind.Arrow)]
    [InlineData(CursorKind.NonText, CursorKind.NonText)]
    [InlineData(CursorKind.Hand, CursorKind.Arrow)]
    public void DragWithoutIBeamAtEitherEnd_IsRejected(CursorKind down, CursorKind up)
    {
        var e = Event(SelectionGesture.DragSelect, down, up);
        Assert.NotNull(SelectionGate.RejectReason(e, requireTextCursor: true));
    }

    [Theory]
    [InlineData(CursorKind.IBeam, CursorKind.Arrow)]
    [InlineData(CursorKind.Arrow, CursorKind.IBeam)]
    public void DragWithIBeamAtOneEnd_IsAllowed(CursorKind down, CursorKind up)
    {
        var e = Event(SelectionGesture.DragSelect, down, up);
        Assert.Null(SelectionGate.RejectReason(e, requireTextCursor: true));
    }

    [Fact]
    public void DoubleClickWithArrow_IsAllowed()
    {
        var e = Event(SelectionGesture.DoubleClick, CursorKind.Arrow, CursorKind.Arrow);
        Assert.Null(SelectionGate.RejectReason(e, requireTextCursor: true));
    }

    [Theory]
    [InlineData(CursorKind.Hand)]
    [InlineData(CursorKind.NonText)]
    public void DoubleClickWithNonTextCursor_IsRejected(CursorKind cursor)
    {
        var e = Event(SelectionGesture.DoubleClick, cursor, cursor);
        Assert.NotNull(SelectionGate.RejectReason(e, requireTextCursor: true));
    }

    [Theory]
    [InlineData(CursorKind.Unknown, CursorKind.NonText)]
    [InlineData(CursorKind.Arrow, CursorKind.Unknown)]
    public void UnknownCursor_NeverBlocks(CursorKind down, CursorKind up)
    {
        var e = Event(SelectionGesture.DragSelect, down, up);
        Assert.Null(SelectionGate.RejectReason(e, requireTextCursor: true));
    }

    [Fact]
    public void CursorGateOff_AllowsNonTextCursor()
    {
        var e = Event(SelectionGesture.DragSelect, CursorKind.NonText, CursorKind.NonText);
        Assert.Null(SelectionGate.RejectReason(e, requireTextCursor: false));
    }

    [Fact]
    public void EventsWithoutSamples_AreAllowed()
    {
        // Default-constructed events (no hook samples) must behave as before the gate.
        var e = new SelectionEvent(0, 0, SelectionGesture.DragSelect);
        Assert.Null(SelectionGate.RejectReason(e, requireTextCursor: true));
    }

    [Fact]
    public void ArrowDrag_IsRejected_UnlessPolicyAllowsIt()
    {
        var e = Event(SelectionGesture.DragSelect, CursorKind.Arrow, CursorKind.Arrow);
        Assert.NotNull(SelectionGate.RejectReason(e, requireTextCursor: true));
        Assert.Null(SelectionGate.RejectReason(e, requireTextCursor: true, allowArrowDrag: true));
    }

    [Fact]
    public void AllowArrowDrag_StillRejectsHandDrag_AndModifiers()
    {
        var hand = Event(SelectionGesture.DragSelect, CursorKind.Hand, CursorKind.Hand);
        Assert.NotNull(SelectionGate.RejectReason(hand, requireTextCursor: true, allowArrowDrag: true));
        var ctrl = Event(SelectionGesture.DragSelect, CursorKind.Arrow, CursorKind.Arrow, modsDown: HeldModifiers.Control);
        Assert.NotNull(SelectionGate.RejectReason(ctrl, requireTextCursor: true, allowArrowDrag: true));
    }
}
