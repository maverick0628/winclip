using WinClip.Native;
using WinClip.Services;

namespace WinClip.Tests.Services;

public class SelectionCapturePolicyTests
{
    [Theory]
    [InlineData(CapturePurpose.Copy, true)]
    [InlineData(CapturePurpose.Copy, false)]
    [InlineData(CapturePurpose.ReadText, false)]
    [InlineData(CapturePurpose.Cut, false)]
    [InlineData(CapturePurpose.Cut, true)]
    public void Terminal_NeverGetsAKeystroke(CapturePurpose purpose, bool uiaHasText)
    {
        var route = SelectionCapturePolicy.Choose(purpose, isTerminal: true, uiaHasText);
        Assert.NotEqual(CaptureRoute.SendKeystroke, route);
    }

    [Fact]
    public void Terminal_WithUiaText_UsesIt()
    {
        Assert.Equal(CaptureRoute.UseUiaText,
            SelectionCapturePolicy.Choose(CapturePurpose.Copy, isTerminal: true, uiaHasText: true));
    }

    [Fact]
    public void Terminal_WithoutUiaText_Skips()
    {
        Assert.Equal(CaptureRoute.Skip,
            SelectionCapturePolicy.Choose(CapturePurpose.Copy, isTerminal: true, uiaHasText: false));
    }

    [Theory]
    [InlineData(CapturePurpose.Copy)]
    [InlineData(CapturePurpose.Cut)]
    public void OtherApps_RealCopyAndCut_KeepTheKeystroke(CapturePurpose purpose)
    {
        // Ctrl+C keeps rich formatting; Ctrl+X is the only way to remove the text.
        Assert.Equal(CaptureRoute.SendKeystroke,
            SelectionCapturePolicy.Choose(purpose, isTerminal: false, uiaHasText: true));
    }

    [Fact]
    public void OtherApps_ReadText_PrefersUia()
    {
        Assert.Equal(CaptureRoute.UseUiaText,
            SelectionCapturePolicy.Choose(CapturePurpose.ReadText, isTerminal: false, uiaHasText: true));
    }

    [Fact]
    public void OtherApps_ReadText_FallsBackToKeystroke()
    {
        Assert.Equal(CaptureRoute.SendKeystroke,
            SelectionCapturePolicy.Choose(CapturePurpose.ReadText, isTerminal: false, uiaHasText: false));
    }

    [Theory]
    [InlineData(CapturePurpose.Copy, false, false)]
    [InlineData(CapturePurpose.Cut, false, false)]
    [InlineData(CapturePurpose.ReadText, false, true)]
    [InlineData(CapturePurpose.Copy, true, true)]
    [InlineData(CapturePurpose.ReadText, true, true)]
    [InlineData(CapturePurpose.Cut, true, false)]
    public void ShouldReadUia_OnlyWhenItCanChangeTheRoute(CapturePurpose purpose, bool isTerminal, bool expected)
    {
        Assert.Equal(expected, SelectionCapturePolicy.ShouldReadUia(purpose, isTerminal));
    }

    [Theory]
    [InlineData("ConsoleWindowClass", true)]
    [InlineData("CASCADIA_HOSTING_WINDOW_CLASS", true)]
    [InlineData("mintty", true)]
    [InlineData("Chrome_WidgetWin_1", false)]
    [InlineData("Notepad", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TerminalClasses_AreRecognised(string? className, bool expected)
    {
        Assert.Equal(expected, TerminalWindows.IsTerminalClass(className));
    }

    [Fact]
    public async Task UiaRead_RealCall_DoesNotThrow()
    {
        // In CI the focused element may be anything, so any status is acceptable
        _ = await UiaSelectionReader.ReadAsync(TimeSpan.FromMilliseconds(300));
    }

    // A real UIA read from another test can still hold the single in-flight slot
    private static async Task WaitForIdleReaderAsync()
    {
        for (int i = 0; i < 600 && !UiaSelectionReader.IsIdle; i++)
            await Task.Delay(50);
        Assert.True(UiaSelectionReader.IsIdle, "an earlier UIA read never finished");
    }

    [Fact]
    public async Task UiaRead_BlockedReader_GivesUpAtBudget_AndRefusesOverlap()
    {
        await WaitForIdleReaderAsync();
        using var release = new ManualResetEventSlim();
        var blocked = UiaSelectionReader.ReadAsync(TimeSpan.FromMilliseconds(50), () =>
        {
            release.Wait();
            return new UiaSelection(UiaSelectionStatus.Text, "late");
        });
        var guard = Task.Delay(TimeSpan.FromSeconds(10));
        Assert.Same(blocked, await Task.WhenAny(blocked, guard));
        Assert.Equal(UiaSelectionStatus.Unavailable, (await blocked).Status);

        // The first read is still stuck, so a second one gives up at once
        var overlap = await UiaSelectionReader.ReadAsync(TimeSpan.FromSeconds(5),
            () => new UiaSelection(UiaSelectionStatus.Text, "never"));
        Assert.Equal(UiaSelectionStatus.Unavailable, overlap.Status);

        release.Set();
        await WaitForIdleReaderAsync();
        var after = await UiaSelectionReader.ReadAsync(TimeSpan.FromSeconds(5),
            () => new UiaSelection(UiaSelectionStatus.Text, "next"));
        Assert.Equal("next", after.Text);
    }

    [Fact]
    public async Task UiaRead_ThrowingReader_IsUnavailable()
    {
        await WaitForIdleReaderAsync();
        bool ran = false;
        var result = await UiaSelectionReader.ReadAsync(TimeSpan.FromSeconds(5),
            () =>
            {
                ran = true;
                throw new InvalidOperationException("element vanished");
            });
        Assert.True(ran);
        Assert.Equal(UiaSelectionStatus.Unavailable, result.Status);
    }
}
