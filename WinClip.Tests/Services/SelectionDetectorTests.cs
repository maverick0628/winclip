using WinClip.Services;

namespace WinClip.Tests.Services;

public class SelectionDetectorTests
{
    private static readonly DateTime T0 = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FirstClick_IsNotASecondClick()
    {
        var detector = new SelectionDetector();
        Assert.False(detector.IsSecondClick(100, 100, T0));
    }

    [Fact]
    public void ClickSoonAfterAClick_IsASecondClick()
    {
        var detector = new SelectionDetector();
        detector.OnMouseDown(100, 100, T0);
        Assert.Equal(SelectionGesture.None, detector.OnMouseUp(100, 100, T0.AddMilliseconds(50)));

        Assert.True(detector.IsSecondClick(102, 101, T0.AddMilliseconds(200)));
        Assert.False(detector.IsSecondClick(100, 100, T0.AddMilliseconds(800)));
        Assert.False(detector.IsSecondClick(150, 100, T0.AddMilliseconds(200)));
    }

    [Fact]
    public void AfterADoubleClick_TheNextClickStartsANewSequence()
    {
        var detector = new SelectionDetector();
        detector.OnMouseDown(100, 100, T0);
        detector.OnMouseUp(100, 100, T0.AddMilliseconds(50));
        detector.OnMouseDown(100, 100, T0.AddMilliseconds(150));
        Assert.Equal(SelectionGesture.DoubleClick, detector.OnMouseUp(100, 100, T0.AddMilliseconds(200)));

        Assert.False(detector.IsSecondClick(100, 100, T0.AddMilliseconds(300)));
    }
}
