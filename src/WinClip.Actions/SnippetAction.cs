using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace WinClip.Actions;

/// <summary>
/// Saves selected text as a named snippet for quick recall.
/// Snippets are persisted to snippets.json.
/// </summary>
public class SnippetAction : IAction
{
    private readonly SnippetStore _store;

    public SnippetAction(SnippetStore store)
    {
        _store = store;
    }

    public string Name => "Save Snippet";
    public string Description => "Save selected text as a reusable snippet";
    public string IconName => "Snippet";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        if (string.IsNullOrWhiteSpace(selectedText))
            return Task.FromResult(ActionResult.FailureResult("No text to save as snippet"));

        // Auto-generate a name from the content
        var name = selectedText.Length <= 30
            ? selectedText.ReplaceLineEndings(" ").Trim()
            : selectedText[..27].ReplaceLineEndings(" ").Trim() + "...";

        _store.Add(name, selectedText);
        return Task.FromResult(ActionResult.SuccessResult(
            $"Snippet saved ({_store.Count} total)"));
    }
}

/// <summary>
/// Persistent storage for text snippets.
/// Backed by a JSON file in %APPDATA%/WinClip/.
/// </summary>
public class SnippetStore
{
    private readonly string _filePath;
    private readonly List<Snippet> _snippets = new();
    private readonly object _lock = new();

    public SnippetStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public int Count
    {
        get { lock (_lock) return _snippets.Count; }
    }

    public void Add(string name, string text)
    {
        lock (_lock)
        {
            // Replace existing with same name
            _snippets.RemoveAll(s => s.Name == name);
            _snippets.Insert(0, new Snippet(name, text, DateTime.UtcNow));
            Save();
        }
    }

    public bool Remove(string name)
    {
        lock (_lock)
        {
            var removed = _snippets.RemoveAll(s => s.Name == name) > 0;
            if (removed) Save();
            return removed;
        }
    }

    public Snippet? Get(string name)
    {
        lock (_lock)
            return _snippets.FirstOrDefault(s => s.Name == name);
    }

    public IReadOnlyList<Snippet> GetAll()
    {
        lock (_lock)
            return _snippets.ToList().AsReadOnly();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<List<Snippet>>(json);
                if (loaded != null)
                    _snippets.AddRange(loaded);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load snippets from '{_filePath}': {ex.Message}");
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_snippets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save snippets to '{_filePath}': {ex.Message}");
        }
    }
}

public sealed record Snippet(string Name, string Text, DateTime CreatedAt);
