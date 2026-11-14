using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PinnedHistoryStore"/>.
/// </summary>
public class PinnedHistoryStoreTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly PinnedHistoryStore _store;

    public PinnedHistoryStoreTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"winclip_pinned_test_{Guid.NewGuid()}.json");
        _store = new PinnedHistoryStore(_testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    [Fact]
    public void Add_NewEntry_Persists()
    {
        _store.Add("hello world");

        Assert.Equal(1, _store.Count);
        Assert.True(_store.Contains("hello world"));
        Assert.True(File.Exists(_testFilePath));
    }

    [Fact]
    public void Add_DuplicateText_MovesToTop()
    {
        _store.Add("first");
        _store.Add("second");
        _store.Add("first"); // re-pin existing

        Assert.Equal(2, _store.Count);
        var all = _store.GetAll();
        Assert.Equal("first", all[0].Text);
        Assert.Equal("second", all[1].Text);
    }

    [Fact]
    public void Add_NullOrEmpty_Ignored()
    {
        _store.Add(string.Empty);
        _store.Add(null!);

        Assert.Equal(0, _store.Count);
    }

    [Fact]
    public void Remove_Existing_ReturnsTrue()
    {
        _store.Add("to-remove");

        var removed = _store.Remove("to-remove");

        Assert.True(removed);
        Assert.Equal(0, _store.Count);
        Assert.False(_store.Contains("to-remove"));
    }

    [Fact]
    public void Remove_NonExistent_ReturnsFalse()
    {
        var removed = _store.Remove("never-pinned");

        Assert.False(removed);
        Assert.Equal(0, _store.Count);
    }

    [Fact]
    public void Contains_EmptyText_ReturnsFalse()
    {
        Assert.False(_store.Contains(string.Empty));
        Assert.False(_store.Contains(null!));
    }

    [Fact]
    public void GetAll_ReturnsNewestFirst()
    {
        _store.Add("first");
        _store.Add("second");
        _store.Add("third");

        var all = _store.GetAll();

        Assert.Equal(3, all.Count);
        Assert.Equal("third", all[0].Text);
        Assert.Equal("second", all[1].Text);
        Assert.Equal("first", all[2].Text);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _store.Add("a");
        _store.Add("b");
        _store.Add("c");

        _store.Clear();

        Assert.Equal(0, _store.Count);
        Assert.Empty(_store.GetAll());
    }

    [Fact]
    public void Load_EmptyFile_ReturnsEmptyList()
    {
        File.WriteAllText(_testFilePath, string.Empty);

        var fresh = new PinnedHistoryStore(_testFilePath);

        Assert.Equal(0, fresh.Count);
    }

    [Fact]
    public void Load_CorruptedFile_HandlesGracefully()
    {
        File.WriteAllText(_testFilePath, "{not valid json[[");

        var fresh = new PinnedHistoryStore(_testFilePath);

        Assert.Equal(0, fresh.Count);
    }

    [Fact]
    public void Persistence_SurvivesRestart()
    {
        _store.Add("survivor 1");
        _store.Add("survivor 2");

        var reloaded = new PinnedHistoryStore(_testFilePath);

        Assert.Equal(2, reloaded.Count);
        Assert.True(reloaded.Contains("survivor 1"));
        Assert.True(reloaded.Contains("survivor 2"));
    }

    [Fact]
    public void Preview_TruncatesLongText()
    {
        var longText = new string('x', 100);
        _store.Add(longText);

        var entry = _store.GetAll()[0];

        Assert.Equal(50, entry.Preview.Length);
        Assert.EndsWith("...", entry.Preview);
    }

    [Fact]
    public void Preview_ReplacesNewlines()
    {
        _store.Add("line one\nline two");

        var entry = _store.GetAll()[0];

        Assert.DoesNotContain("\n", entry.Preview);
        Assert.Contains("line one line two", entry.Preview);
    }

    [Fact]
    public async Task ConcurrentAddRemove_DoesNotThrow()
    {
        var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
        {
            _store.Add($"item-{i}");
            if (i % 3 == 0) _store.Remove($"item-{i}");
            _ = _store.GetAll();
        })).ToArray();

        await Task.WhenAll(tasks);

        // Survives concurrency without exceptions; final count is bounded by adds.
        Assert.True(_store.Count >= 0 && _store.Count <= 50);
    }
}
