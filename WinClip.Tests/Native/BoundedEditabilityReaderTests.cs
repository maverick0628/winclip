using WinClip.Native;

namespace WinClip.Tests.Native;

public class BoundedEditabilityReaderTests
{
    [Fact]
    public async Task HungProviderTimesOutWithoutStartingMoreReads()
    {
        var reader = new BoundedEditabilityReader();
        using var release = new ManualResetEventSlim();
        using var entered = new ManualResetEventSlim();
        try
        {
            var result = reader.ReadAsync(() => { entered.Set(); release.Wait(); return true; },
                TimeSpan.FromMilliseconds(30));
            Assert.True(entered.Wait(TimeSpan.FromSeconds(2)));
            Assert.Null(await result.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Null(await reader.ReadAsync(() => throw new Exception("Must not start"), TimeSpan.FromSeconds(1)));
            Assert.False(reader.IsIdle);
        }
        finally
        {
            release.Set();
            Assert.True(SpinWait.SpinUntil(() => reader.IsIdle, TimeSpan.FromSeconds(2)));
        }
        Assert.True(await reader.ReadAsync(() => true, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task CompletedReadIsDiscardedWhenFocusOrInputChanged()
    {
        var reader = new BoundedEditabilityReader();
        Assert.Null(await reader.ReadAsync(() => true, TimeSpan.FromSeconds(2), () => false));
        Assert.True(await reader.ReadAsync(() => true, TimeSpan.FromSeconds(2), () => true));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StaleInputAbortsTheGestureRatherThanActingLikeReadOnly(bool editable)
    {
        var result = EditabilityResult.FromRead(editable, isCurrent: false);
        Assert.False(result.IsCurrent);
        Assert.False(result.IsEditable);
    }

    [Fact]
    public void TimeoutCanStillShowNonDestructiveActionsWhenInputIsCurrent()
    {
        var result = EditabilityResult.FromRead(null, isCurrent: true);
        Assert.True(result.IsCurrent);
        Assert.False(result.IsEditable);
    }

    [Fact]
    public async Task ProviderFailureReturnsUnknownAndReleasesTheSlot()
    {
        var reader = new BoundedEditabilityReader();
        Assert.Null(await reader.ReadAsync(() => throw new InvalidOperationException(), TimeSpan.FromSeconds(2)));
        Assert.True(reader.IsIdle);
        Assert.False(await reader.ReadAsync(() => false, TimeSpan.FromSeconds(2)));
    }
}
