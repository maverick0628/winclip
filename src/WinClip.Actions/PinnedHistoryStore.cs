using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace WinClip.Actions;

/// <summary>
/// Persistent set of pinned clipboard-history entries that survive eviction
/// from the in-memory ring buffer. Backed by a JSON file in %APPDATA%\WinClip\.
/// Mirrors the <see cref="SnippetStore"/> shape: thread-safe, best-effort
/// persistence (errors logged via <see cref="Debug"/>, never thrown to callers).
/// </summary>
public class PinnedHistoryStore
{
    private readonly string _filePath;
    private readonly List<PinnedEntry> _entries = new();
    private readonly object _lock = new();

    public PinnedHistoryStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public int Count
    {
        get { lock (_lock) return _entries.Count; }
    }

    /// <summary>
    /// Pins the given text. If already pinned, moves it to the top.
    /// Pin uniqueness is determined by exact text match.
    /// </summary>
    public void Add(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        lock (_lock)
        {
            _entries.RemoveAll(e => e.Text == text);
            _entries.Insert(0, new PinnedEntry(text, DateTime.UtcNow));
            Save();
        }
    }

    /// <summary>
    /// Removes a pinned entry by exact text match.
    /// Returns true if an entry was removed.
    /// </summary>
    public bool Remove(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        lock (_lock)
        {
            var removed = _entries.RemoveAll(e => e.Text == text) > 0;
            if (removed) Save();
            return removed;
        }
    }

    /// <summary>True if the given text is currently pinned (exact match).</summary>
    public bool Contains(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        lock (_lock) return _entries.Any(e => e.Text == text);
    }

    public IReadOnlyList<PinnedEntry> GetAll()
    {
        lock (_lock) return _entries.ToList().AsReadOnly();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            Save();
        }
    }

    private void Load()
    {
        try
        {
            var json = HistoryFileProtector.ReadOrNull(_filePath);
            if (string.IsNullOrEmpty(json)) return;
            var loaded = JsonSerializer.Deserialize<List<PinnedEntry>>(json);
            if (loaded != null)
                _entries.AddRange(loaded);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load pinned history from '{_filePath}': {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            HistoryFileProtector.Write(_filePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save pinned history to '{_filePath}': {ex.Message}");
        }
    }
}

public sealed record PinnedEntry(string Text, DateTime PinnedAt)
{
    /// <summary>Single-line preview, truncated to 50 chars.</summary>
    public string Preview => Text.Length <= 50
        ? Text.ReplaceLineEndings(" ")
        : Text[..47].ReplaceLineEndings(" ") + "...";
}
