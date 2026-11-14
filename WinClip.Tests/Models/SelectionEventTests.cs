using WinClip.Models;
using WinClip.Native;
using Xunit;

namespace WinClip.Tests.Models;

public class SelectionEventTests
{
    [Fact]
    public void Constructor_SetsCoordinatesAndDoubleClick()
    {
        var evt = new SelectionEvent(100, 200, isDoubleClick: true);

        Assert.Equal(100, evt.CursorX);
        Assert.Equal(200, evt.CursorY);
        Assert.True(evt.IsDoubleClick);
    }

    [Fact]
    public void Constructor_DefaultIsNotDoubleClick()
    {
        var evt = new SelectionEvent(0, 0, isDoubleClick: false);

        Assert.False(evt.IsDoubleClick);
    }

    [Fact]
    public void Constructor_SetsTimestamp()
    {
        var before = DateTime.UtcNow;
        var evt = new SelectionEvent(100, 200, isDoubleClick: true);
        var after = DateTime.UtcNow;

        Assert.InRange(evt.Timestamp, before, after);
    }

    [Fact]
    public void PointConstructor_MapsXY()
    {
        var pt = new POINT { x = 300, y = 400 };
        var evt = new SelectionEvent(pt, isDoubleClick: true);

        Assert.Equal(300, evt.CursorX);
        Assert.Equal(400, evt.CursorY);
        Assert.True(evt.IsDoubleClick);
    }

    [Fact]
    public void NegativeCoordinates_AreAllowed()
    {
        // Multi-monitor setups can have negative coordinates
        var evt = new SelectionEvent(-100, -50, isDoubleClick: false);

        Assert.Equal(-100, evt.CursorX);
        Assert.Equal(-50, evt.CursorY);
    }

    [Fact]
    public void ZeroCoordinates_AreValid()
    {
        var evt = new SelectionEvent(0, 0, isDoubleClick: true);

        Assert.Equal(0, evt.CursorX);
        Assert.Equal(0, evt.CursorY);
    }
}
