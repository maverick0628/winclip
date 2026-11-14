using System.Text;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that converts CSV or TSV text into a Markdown table.
/// Contextual: only appears when text contains comma or tab-separated multi-line data.
/// </summary>
public class MarkdownTableAction : IAction, IContextualAction
{
    public string Name => "Markdown Table";
    public string Description => "Convert CSV/TSV data to a Markdown table";
    public string IconName => "Table";

    public bool IsAvailable() => true;

    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        if (!selectedText.Contains('\n')) return false;

        var firstLine = selectedText.Split(["\r\n", "\n"], StringSplitOptions.None)[0];
        return firstLine.Contains(',') || firstLine.Contains('\t');
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            var lines = selectedText.Split(["\r\n", "\n"], StringSplitOptions.None)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (lines.Length < 1)
                return Task.FromResult(ActionResult.FailureResult("Need at least a header row"));

            // Detect delimiter: tab takes priority over comma
            var delimiter = lines[0].Contains('\t') ? '\t' : ',';

            // Parse all rows
            var rows = lines.Select(l => ParseRow(l, delimiter)).ToArray();

            // Determine column count and widths
            var colCount = rows.Max(r => r.Length);
            var colWidths = new int[colCount];

            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    colWidths[i] = Math.Max(colWidths[i], row[i].Length);
                }
            }

            // Ensure minimum width of 3 for separator row
            for (int i = 0; i < colWidths.Length; i++)
                colWidths[i] = Math.Max(colWidths[i], 3);

            var sb = new StringBuilder();

            // Header row
            sb.Append("| ");
            for (int i = 0; i < colCount; i++)
            {
                var cell = i < rows[0].Length ? rows[0][i] : "";
                sb.Append(cell.PadRight(colWidths[i]));
                sb.Append(" | ");
            }
            sb.AppendLine();

            // Separator row
            sb.Append("| ");
            for (int i = 0; i < colCount; i++)
            {
                sb.Append(new string('-', colWidths[i]));
                sb.Append(" | ");
            }
            sb.AppendLine();

            // Data rows
            for (int r = 1; r < rows.Length; r++)
            {
                sb.Append("| ");
                for (int i = 0; i < colCount; i++)
                {
                    var cell = i < rows[r].Length ? rows[r][i] : "";
                    sb.Append(cell.PadRight(colWidths[i]));
                    sb.Append(" | ");
                }
                sb.AppendLine();
            }

            var result = sb.ToString().TrimEnd('\r', '\n');
            ClipboardService.Set(result);

            return Task.FromResult(ActionResult.SuccessResult(
                $"Markdown table ({rows.Length} rows, {colCount} cols) copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to create Markdown table", ex));
        }
    }

    private static string[] ParseRow(string line, char delimiter)
    {
        if (delimiter == ',')
            return ParseCsvRow(line);

        // Simple split for tab-delimited
        return line.Split(delimiter).Select(f => f.Trim()).ToArray();
    }

    private static string[] ParseCsvRow(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
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
}
