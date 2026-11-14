using WinClip.Services;

namespace WinClip.Tests.Services;

public class ClipboardChangeWaiterTests
{
    private static Task NoDelay(int _) => Task.CompletedTask;

    [Fact]
    public async Task SameTextAsBefore_StillCountsWhenSequenceMoves()
    {
        // Regression: double-clicking a word already on the clipboard used to show no popup,
        // because the old check compared text instead of the sequence number.
        uint sequence = 10;
        var result = await ClipboardChangeWaiter.WaitForNewTextAsync(
            10,
            () => sequence,
            () => "hello",
            _ => { sequence = 11; return Task.CompletedTask; });

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task UnchangedSequence_ReturnsNullEvenWithTextOnClipboard()
    {
        var result = await ClipboardChangeWaiter.WaitForNewTextAsync(
            10, () => 10, () => "stale text", NoDelay);

        Assert.Null(result);
    }

    [Fact]
    public async Task SequenceChangesLate_WaitsForIt()
    {
        int polls = 0;
        var result = await ClipboardChangeWaiter.WaitForNewTextAsync(
            10,
            () => polls >= 3 ? 11u : 10u,
            () => "late",
            _ => { polls++; return Task.CompletedTask; });

        Assert.Equal("late", result);
        Assert.Equal(3, polls);
    }

    [Fact]
    public async Task TextArrivesAfterEmptyStep_KeepsPolling()
    {
        // Apps may empty the clipboard first, then set it.
        int polls = 0;
        var result = await ClipboardChangeWaiter.WaitForNewTextAsync(
            10,
            () => 11,
            () => polls >= 2 ? "set" : null,
            _ => { polls++; return Task.CompletedTask; });

        Assert.Equal("set", result);
    }

    [Fact]
    public async Task ChangedToNonText_ReturnsNull()
    {
        var result = await ClipboardChangeWaiter.WaitForNewTextAsync(
            10, () => 11, () => "   ", NoDelay);

        Assert.Null(result);
    }

    [Fact]
    public async Task UsesEveryDelayInOrder()
    {
        var seen = new List<int>();
        await ClipboardChangeWaiter.WaitForNewTextAsync(
            10, () => 10, () => null,
            ms => { seen.Add(ms); return Task.CompletedTask; },
            [1, 2, 3]);

        Assert.Equal(new[] { 1, 2, 3 }, seen);
    }
}
