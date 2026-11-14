using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that removes empty and whitespace-only lines from text.
/// Contextual: only appears for multi-line text.
/// </summary>
public class RemoveBlankLinesAction : IAction, IContextualAction
{
    public string Name => "Remove Blank Lines";
    public string Description => "Strip empty and whitespace-only lines";
    public string IconName => "Clean";

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
            var originalCount = lines.Length;

            var filtered = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

            if (filtered.Length == originalCount)
                return Task.FromResult(ActionResult.SuccessResult("No blank lines found"));

            var result = string.Join(Environment.NewLine, filtered);
            ClipboardService.Set(result);

            var removed = originalCount - filtered.Length;
            return Task.FromResult(ActionResult.SuccessResult(
                $"Removed {removed} blank line{(removed != 1 ? "s" : "")} ({filtered.Length} remaining)"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to remove blank lines", ex));
        }
    }
}
