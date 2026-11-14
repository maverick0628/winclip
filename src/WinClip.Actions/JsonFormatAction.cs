using System.Text.Json;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that formats or minifies JSON text.
/// Implements IContextualAction to only appear when text looks like JSON.
/// </summary>
public class JsonFormatAction : IAction, IContextualAction
{
    private readonly JsonFormatMode _mode;

    public JsonFormatAction(JsonFormatMode mode)
    {
        _mode = mode;
    }

    public string Name => _mode switch
    {
        JsonFormatMode.Prettify => "JSON Prettify",
        JsonFormatMode.Minify => "JSON Minify",
        _ => "JSON Format"
    };

    public string Description => _mode switch
    {
        JsonFormatMode.Prettify => "Format JSON with indentation",
        JsonFormatMode.Minify => "Minify JSON to a single line",
        _ => "Format JSON"
    };

    public string IconName => "Code";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show when text looks like JSON (starts with { or [).
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        var trimmed = selectedText.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected to format"));

            // Parse the JSON to validate it
            using var doc = JsonDocument.Parse(selectedText);

            var options = new JsonSerializerOptions
            {
                WriteIndented = _mode == JsonFormatMode.Prettify
            };

            // Re-serialize with desired formatting
            string result;
            using (var stream = new System.IO.MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
                {
                    Indented = _mode == JsonFormatMode.Prettify,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }))
                {
                    doc.RootElement.WriteTo(writer);
                }
                result = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }

            ClipboardService.Set(result);

            var label = _mode == JsonFormatMode.Prettify ? "prettified" : "minified";
            return Task.FromResult(ActionResult.SuccessResult($"JSON {label} and copied to clipboard"));
        }
        catch (JsonException)
        {
            return Task.FromResult(ActionResult.FailureResult("Invalid JSON — could not parse"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to format JSON", ex));
        }
    }
}

public enum JsonFormatMode
{
    Prettify,
    Minify
}
