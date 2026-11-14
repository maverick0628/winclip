using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that converts between CSV and JSON array-of-objects format.
/// Contextual: detects CSV (comma-separated lines) or JSON arrays and converts
/// to the other format. CSV uses the first row as headers.
/// </summary>
public class CsvJsonConvertAction : IAction, IContextualAction
{
    public string Name => "CSV \u2194 JSON";
    public string Description => "Convert between CSV and JSON array formats";
    public string IconName => "Table";

    public bool IsAvailable() => true;

    /// <summary>
    /// Show when text looks like CSV (multiple comma-separated lines) or a JSON array.
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        return IsJsonArray(selectedText) || IsCsv(selectedText);
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            if (IsJsonArray(selectedText))
                return Task.FromResult(JsonToCsv(selectedText));
            if (IsCsv(selectedText))
                return Task.FromResult(CsvToJson(selectedText));

            return Task.FromResult(ActionResult.FailureResult("Text is not recognized as CSV or JSON array"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to convert", ex));
        }
    }

    private static bool IsJsonArray(string text)
    {
        var trimmed = text.TrimStart();
        return trimmed.StartsWith('[');
    }

    private static bool IsCsv(string text)
    {
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None)
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        // Need at least a header + one data row, and first line should contain commas
        return lines.Length >= 2 && lines[0].Contains(',');
    }

    private static ActionResult CsvToJson(string csv)
    {
        var lines = csv.Split(["\r\n", "\n"], StringSplitOptions.None)
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length < 2)
            return ActionResult.FailureResult("CSV needs at least a header row and one data row");

        var headers = ParseCsvLine(lines[0]);

        // Handle duplicate headers by appending _2, _3, etc.
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            var key = headers[i];
            if (seen.TryGetValue(key, out var count))
            {
                seen[key] = count + 1;
                headers[i] = $"{key}_{count + 1}";
            }
            else
            {
                seen[key] = 1;
            }
        }

        var records = new List<Dictionary<string, string>>();

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            var record = new Dictionary<string, string>();

            for (int j = 0; j < headers.Length; j++)
            {
                var value = j < values.Length ? values[j] : "";
                record[headers[j]] = value;
            }

            records.Add(record);
        }

        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        ClipboardService.Set(json);

        return ActionResult.SuccessResult($"CSV \u2192 JSON ({records.Count} records) copied to clipboard");
    }

    private static ActionResult JsonToCsv(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            return ActionResult.FailureResult("JSON must be a non-empty array of objects");

        // Collect all unique keys from all objects for headers
        var allKeys = new List<string>();
        var keySet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                return ActionResult.FailureResult("Each array element must be an object");

            foreach (var prop in item.EnumerateObject())
            {
                if (keySet.Add(prop.Name))
                    allKeys.Add(prop.Name);
            }
        }

        var lines = new List<string> { string.Join(",", allKeys.Select(EscapeCsvField)) };

        foreach (var item in root.EnumerateArray())
        {
            var values = allKeys.Select(key =>
            {
                if (item.TryGetProperty(key, out var val))
                {
                    return val.ValueKind switch
                    {
                        JsonValueKind.String => val.GetString() ?? "",
                        JsonValueKind.Null => "",
                        _ => val.GetRawText()
                    };
                }
                return "";
            });

            lines.Add(string.Join(",", values.Select(EscapeCsvField)));
        }

        var result = string.Join(Environment.NewLine, lines);
        ClipboardService.Set(result);

        return ActionResult.SuccessResult(
            $"JSON \u2192 CSV ({root.GetArrayLength()} rows, {allKeys.Count} columns) copied to clipboard");
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote ""
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString().Trim());
        return fields.ToArray();
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
