using System.Windows;
using WinClip.UI;

namespace WinClip.Tests.UI;

public class PopupPositionerTests
{
    [Theory]
    [InlineData(1920, 0, 3840, 2160, 0.5, 0.5, 4200, 1000)]
    [InlineData(-2560, -200, 2560, 1440, 0.6666666667, 0.6666666667, -2500, -180)]
    [InlineData(0, 1080, 1920, 1080, 1, 1, 1900, 2140)]
    public void PopupStaysOnTargetMonitor(double x, double y, double width, double height,
        double scaleX, double scaleY, int cursorX, int cursorY)
    {
        var monitor = MonitorGeometry.FromPixels(new Rect(x, y, width, height), scaleX, scaleY);
        var position = PopupPositioner.Calculate(cursorX, cursorY, scaleX, scaleY, 360, 60, monitor.WorkArea);

        Assert.InRange(position.Left, monitor.WorkArea.Left + 8, monitor.WorkArea.Right - 368);
        Assert.InRange(position.Top, monitor.WorkArea.Top + 8, monitor.WorkArea.Bottom - 68);
    }

    [Fact]
    public void HistoryCentersInTheScaledSecondaryWorkArea()
    {
        var monitor = MonitorGeometry.FromPixels(new Rect(-3840, 200, 3840, 2160), 0.5, 0.5);
        var position = monitor.Center(540, 520);

        Assert.Equal(-1230, position.Left);
        Assert.Equal(380, position.Top);
    }

    [Fact]
    public void PopupFlipsBelowCursorNearTopEdge()
    {
        var position = PopupPositioner.Calculate(600, 5, 1, 1, 360, 60, new Rect(0, 0, 1920, 1040));
        Assert.Equal(17, position.Top);
    }
}
