using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that applies a regex find-and-replace to selected text.
/// The first line of the selection is the pattern and replacement separated by ";;".
/// The remaining lines (or the entire text if no separator) are the target.
///
/// Usage formats:
///   pattern;;replacement
///   target text here...
///
/// If the text contains no ";;" separator, prompts via the result message.
/// </summary>
public class RegexReplaceAction : IAction
{
    private const string Separator = ";;";

    public string Name => "Regex Replace";
    public string Description => "Find and replace with regex (first line: pattern;;replacement)";
    public string IconName => "Find";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            // Split into first line (command) and rest (target)
            var newlineIndex = selectedText.IndexOf('\n');
            if (newlineIndex < 0)
            {
                return Task.FromResult(ActionResult.FailureResult(
                    "Format: first line is pattern;;replacement, remaining lines are the target text"));
            }

            var firstLine = selectedText[..newlineIndex].TrimEnd('\r');
            var target = selectedText[(newlineIndex + 1)..];

            // Parse pattern;;replacement from first line
            var sepIndex = firstLine.IndexOf(Separator, StringComparison.Ordinal);
            if (sepIndex < 0)
            {
                return Task.FromResult(ActionResult.FailureResult(
                    "First line must contain pattern;;replacement (separate with ;;)"));
            }

            var pattern = firstLine[..sepIndex];
            var replacement = firstLine[(sepIndex + Separator.Length)..];

            if (string.IsNullOrEmpty(pattern))
                return Task.FromResult(ActionResult.FailureResult("Regex pattern cannot be empty"));

            // Apply regex replace with a timeout to prevent catastrophic backtracking
            var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(2));
            var result = regex.Replace(target, replacement);
            var matchCount = regex.Matches(target).Count;

            ClipboardService.Set(result);

            return Task.FromResult(ActionResult.SuccessResult(
                $"Replaced {matchCount} match{(matchCount != 1 ? "es" : "")} — result copied to clipboard"));
        }
        catch (RegexMatchTimeoutException)
        {
            return Task.FromResult(ActionResult.FailureResult("Regex timed out — pattern may cause excessive backtracking"));
        }
        catch (ArgumentException ex) when (ex.Message.Contains("parse") || ex.Message.Contains("pattern"))
        {
            return Task.FromResult(ActionResult.FailureResult($"Invalid regex pattern: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to apply regex replace", ex));
        }
    }
}
