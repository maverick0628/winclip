using WinClip.UI;

namespace WinClip.Tests.UI;

public class HistoryClipboardTests
{
    [Fact]
    public void FailedWriteNeverReportsCopied()
        => Assert.False(HistoryClipboard.TryCopy("clip", _ => false));

    [Fact]
    public void LockedClipboardNeverReportsCopied()
        => Assert.False(HistoryClipboard.TryCopy("clip", _ => throw new System.Runtime.InteropServices.ExternalException()));

    [Fact]
    public void SuccessfulWriteUsesTheSelectedText()
    {
        string? written = null;
        Assert.True(HistoryClipboard.TryCopy("selected clip", text => { written = text; return true; }));
        Assert.Equal("selected clip", written);
    }
}
