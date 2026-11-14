using System.Text;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that prefixes each line with a sequential line number.
/// Contextual: only appears for multi-line text.
/// </summary>
public class AddLineNumbersAction : IAction, IContextualAction
{
    public string Name => "Add Line Numbers";
    public string Description => "Prefix each line with a sequential number";
    public string IconName => "List";

    public bool IsAvailable() => true;

    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        return selectedText.Contains('\n');
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            var lines = selectedText.Split(["\r\n", "\n"], StringSplitOptions.None);
            var width = lines.Length.ToString().Length; // pad to widest number

            var sb = new StringBuilder(selectedText.Length + lines.Length * (width + 2));
            for (int i = 0; i < lines.Length; i++)
            {
                var num = (i + 1).ToString().PadLeft(width);
                sb.Append(num).Append("  ").AppendLine(lines[i]);
            }

            // Remove trailing newline added by AppendLine
            var result = sb.ToString().TrimEnd('\r', '\n');

            ClipboardService.Set(result);

            return Task.FromResult(ActionResult.SuccessResult($"Numbered {lines.Length} lines — copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to add line numbers", ex));
        }
    }
}
