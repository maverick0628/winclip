using System.IO;
using System.Text.Json;

namespace WinClip.Actions;

/// <summary>
/// Maintains a history of clipboard entries.
/// The action copies the selected text and adds it to the history ring.
/// History can be accessed from the tray menu or popup.
/// </summary>
public class ClipboardHistoryAction : IAction
{
    private readonly ClipboardHistory _history;

    public ClipboardHistoryAction(ClipboardHistory history)
    {
        _history = history;
    }

    public string Name => "Clipboard History";
    public string Description => "Save to clipboard history";
    public string IconName => "History";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        if (string.IsNullOrWhiteSpace(selectedText))
            return Task.FromResult(ActionResult.FailureResult("No text to save"));

        _history.Add(selectedText);
        return Task.FromResult(ActionResult.SuccessResult(
            $"Saved to history ({_history.Count} items)"));
    }
}

/// <summary>
/// In-memory clipboard history ring buffer.
/// Thread-safe for concurrent access from popup and tray.
/// </summary>
public class ClipboardHistory
{
    private readonly List<ClipboardEntry> _entries = new();
    private readonly object _lock = new();
    private readonly int _maxEntries;
    private readonly string? _filePath;

    public ClipboardHistory(int maxEntries = 50, string? filePath = null)
    {
        _maxEntries = maxEntries;
        _filePath = filePath;
        LoadFromDisk();
    }

    public int Count
    {
        get { lock (_lock) return _entries.Count; }
    }

    public void Add(string text)
    {
        lock (_lock)
        {
            // Remove duplicate if exists
            _entries.RemoveAll(e => e.Text == text);

            _entries.Insert(0, new ClipboardEntry(text, DateTime.UtcNow));

            // Trim to max
            while (_entries.Count > _maxEntries)
                _entries.RemoveAt(_entries.Count - 1);

            SaveToDiskNoThrow();
        }
    }

    public IReadOnlyList<ClipboardEntry> GetAll()
    {
        lock (_lock)
            return _entries.ToList().AsReadOnly();
    }

    public ClipboardEntry? GetAt(int index)
    {
        lock (_lock)
            return index >= 0 && index < _entries.Count ? _entries[index] : null;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            SaveToDiskNoThrow();
        }
    }

    private void LoadFromDisk()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
            return;

        try
        {
            var json = HistoryFileProtector.ReadOrNull(_filePath);
            if (string.IsNullOrEmpty(json)) return;

            var loaded = JsonSerializer.Deserialize<List<ClipboardEntry>>(json);
            if (loaded == null)
                return;

            lock (_lock)
            {
                _entries.Clear();
                _entries.AddRange(loaded.Take(_maxEntries));
            }
        }
        catch
        {
            // Best-effort load; start clean when file is corrupt/unreadable.
        }
    }

    private void SaveToDiskNoThrow()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
            return;

        try
        {
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            HistoryFileProtector.Write(_filePath, json);
        }
        catch
        {
            // Best-effort persistence.
        }
    }
}

public sealed record ClipboardEntry(string Text, DateTime Timestamp)
{
    /// <summary>Preview string (first 50 chars, single line).</summary>
    public string Preview => Text.Length <= 50
        ? Text.ReplaceLineEndings(" ")
        : Text[..47].ReplaceLineEndings(" ") + "...";
}
