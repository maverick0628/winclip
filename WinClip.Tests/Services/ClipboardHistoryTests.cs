using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Services;

/// <summary>
/// Unit tests for the ClipboardHistory and ClipboardEntry classes.
/// </summary>
public class ClipboardHistoryTests
{
    [Fact]
    public void Add_SingleEntry_CanRetrieve()
    {
        // Arrange
        var history = new ClipboardHistory();

        // Act
        history.Add("hello world");

        // Assert
        Assert.Equal(1, history.Count);
        var entry = history.GetAt(0);
        Assert.NotNull(entry);
        Assert.Equal("hello world", entry.Text);
    }

    [Fact]
    public void Add_OverCapacity_EvictsOldest()
    {
        // Arrange
        var history = new ClipboardHistory(maxEntries: 50);

        // Act — add 51 items
        for (int i = 0; i < 51; i++)
        {
            history.Add($"item-{i}");
        }

        // Assert
        Assert.Equal(50, history.Count);

        // The newest item (item-50) should be at index 0
        var newest = history.GetAt(0);
        Assert.NotNull(newest);
        Assert.Equal("item-50", newest.Text);

        // The oldest surviving item should be item-1 (item-0 was evicted)
        var oldest = history.GetAt(49);
        Assert.NotNull(oldest);
        Assert.Equal("item-1", oldest.Text);

        // Verify item-0 is gone — it should not appear anywhere
        var all = history.GetAll();
        Assert.DoesNotContain(all, e => e.Text == "item-0");
    }

    [Fact]
    public void Add_DuplicateText_MovesToTop()
    {
        // Arrange
        var history = new ClipboardHistory();
        history.Add("first");
        history.Add("second");
        history.Add("third");

        // Act — add "first" again (duplicate)
        history.Add("first");

        // Assert — no duplicate, count stays at 3
        Assert.Equal(3, history.Count);

        // "first" should now be at the top (index 0)
        var top = history.GetAt(0);
        Assert.NotNull(top);
        Assert.Equal("first", top.Text);

        // "third" should be at index 1, "second" at index 2
        var second = history.GetAt(1);
        Assert.NotNull(second);
        Assert.Equal("third", second.Text);

        var third = history.GetAt(2);
        Assert.NotNull(third);
        Assert.Equal("second", third.Text);
    }

    [Fact]
    public void GetAll_EmptyHistory_ReturnsEmptyList()
    {
        // Arrange
        var history = new ClipboardHistory();

        // Act
        var all = history.GetAll();

        // Assert
        Assert.NotNull(all);
        Assert.Empty(all);
    }

    [Fact]
    public void GetAll_ReturnsNewestFirst()
    {
        // Arrange
        var history = new ClipboardHistory();
        history.Add("oldest");
        history.Add("middle");
        history.Add("newest");

        // Act
        var all = history.GetAll();

        // Assert
        Assert.Equal(3, all.Count);
        Assert.Equal("newest", all[0].Text);
        Assert.Equal("middle", all[1].Text);
        Assert.Equal("oldest", all[2].Text);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        var history = new ClipboardHistory();
        history.Add("one");
        history.Add("two");
        history.Add("three");
        Assert.Equal(3, history.Count);

        // Act
        history.Clear();

        // Assert
        Assert.Equal(0, history.Count);
        Assert.Empty(history.GetAll());
        Assert.Null(history.GetAt(0));
    }

    [Fact]
    public void Preview_TruncatesLongText()
    {
        // Arrange — text longer than 50 characters
        var longText = new string('A', 100);
        var entry = new ClipboardEntry(longText, DateTime.UtcNow);

        // Act
        var preview = entry.Preview;

        // Assert — should be first 47 chars + "..." = 50 chars total
        Assert.Equal(50, preview.Length);
        Assert.EndsWith("...", preview);
        Assert.Equal(new string('A', 47) + "...", preview);
    }

    [Fact]
    public void Preview_ReplacesNewlinesWithSpaces()
    {
        // Arrange — short text with newlines (under 50 chars so no truncation)
        var textWithNewlines = "line one\r\nline two\nline three";
        var entry = new ClipboardEntry(textWithNewlines, DateTime.UtcNow);

        // Act
        var preview = entry.Preview;

        // Assert — newlines should be replaced with spaces
        Assert.DoesNotContain("\n", preview);
        Assert.DoesNotContain("\r", preview);
        Assert.Contains("line one", preview);
        Assert.Contains("line two", preview);
        Assert.Contains("line three", preview);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAddAndGet_DoNotThrow()
    {
        // Arrange
        var history = new ClipboardHistory(maxEntries: 50);
        var exceptions = new List<Exception>();
        var barrier = new Barrier(participantCount: 4);
        var tasks = new Task[4];

        // Act — run concurrent Add and GetAll operations
        for (int t = 0; t < 4; t++)
        {
            int threadIndex = t;
            tasks[t] = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    for (int i = 0; i < 200; i++)
                    {
                        history.Add($"thread-{threadIndex}-item-{i}");
                        _ = history.GetAll();
                        _ = history.GetAt(0);
                        _ = history.Count;
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert — no exceptions should have been thrown
        Assert.Empty(exceptions);
        Assert.True(history.Count > 0);
        Assert.True(history.Count <= 50);
    }

    [Fact]
    public void Persistence_ReloadsHistoryFromDisk()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"winclip-history-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var historyPath = Path.Combine(tempDir, "clipboard-history.json");

        try
        {
            var writer = new ClipboardHistory(maxEntries: 50, filePath: historyPath);
            writer.Add("first");
            writer.Add("second");

            var reloaded = new ClipboardHistory(maxEntries: 50, filePath: historyPath);
            var all = reloaded.GetAll();

            Assert.Equal(2, all.Count);
            Assert.Equal("second", all[0].Text);
            Assert.Equal("first", all[1].Text);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
