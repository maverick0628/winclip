using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that sorts, deduplicates, or reverses lines in selected text.
/// Implements IContextualAction to only appear for multi-line text.
/// </summary>
public class LineSortAction : IAction, IContextualAction
{
    private readonly LineSortMode _mode;

    public LineSortAction(LineSortMode mode)
    {
        _mode = mode;
    }

    public string Name => _mode switch
    {
        LineSortMode.SortAscending => "Sort Lines A\u2192Z",
        LineSortMode.SortDescending => "Sort Lines Z\u2192A",
        LineSortMode.RemoveDuplicates => "Remove Duplicate Lines",
        LineSortMode.Reverse => "Reverse Lines",
        _ => "Sort Lines"
    };

    public string Description => _mode switch
    {
        LineSortMode.SortAscending => "Sort lines alphabetically (A to Z)",
        LineSortMode.SortDescending => "Sort lines in reverse alphabetical order (Z to A)",
        LineSortMode.RemoveDuplicates => "Remove duplicate lines, keeping first occurrence",
        LineSortMode.Reverse => "Reverse the order of lines",
        _ => "Sort lines"
    };

    public string IconName => "Sort";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show when text contains multiple lines.
    /// </summary>
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
            var originalCount = lines.Length;

            IEnumerable<string> result = _mode switch
            {
                LineSortMode.SortAscending => lines.OrderBy(l => l, StringComparer.OrdinalIgnoreCase),
                LineSortMode.SortDescending => lines.OrderByDescending(l => l, StringComparer.OrdinalIgnoreCase),
                LineSortMode.RemoveDuplicates => lines.Distinct(StringComparer.OrdinalIgnoreCase),
                LineSortMode.Reverse => Enumerable.Reverse(lines),
                _ => lines
            };

            var resultArray = result.ToArray();
            var output = string.Join(Environment.NewLine, resultArray);

            ClipboardService.Set(output);

            var message = _mode switch
            {
                LineSortMode.SortAscending => $"Sorted {originalCount} lines A\u2192Z",
                LineSortMode.SortDescending => $"Sorted {originalCount} lines Z\u2192A",
                LineSortMode.RemoveDuplicates =>
                    $"Removed {originalCount - resultArray.Length} duplicate{(originalCount - resultArray.Length != 1 ? "s" : "")} ({resultArray.Length} unique lines)",
                LineSortMode.Reverse => $"Reversed {originalCount} lines",
                _ => "Lines processed"
            };

            return Task.FromResult(ActionResult.SuccessResult(message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to process lines", ex));
        }
    }
}

public enum LineSortMode
{
    SortAscending,
    SortDescending,
    RemoveDuplicates,
    Reverse
}
