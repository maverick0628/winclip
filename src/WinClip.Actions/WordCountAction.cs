namespace WinClip.Actions;

/// <summary>
/// Action that counts words and characters in the selected text.
/// </summary>
public class WordCountAction : IAction
{
    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public string Name => "Word Count";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public string Description => "Count words and characters in selected text";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public string IconName => "Calculator";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return Task.FromResult(ActionResult.FailureResult("No text selected to count"));
            }

            // Count characters (with spaces)
            int characterCount = selectedText.Length;

            // Count characters without spaces
            int characterCountNoSpaces = selectedText.Count(c => !char.IsWhiteSpace(c));

            // Count words (split by whitespace and filter empty entries)
            string[] words = selectedText.Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            int wordCount = words.Length;

            // Count lines
            int lineCount = selectedText.Split(new[] { '\r', '\n' }, StringSplitOptions.None)
                .Count(line => !string.IsNullOrWhiteSpace(line));

            // Create result message
            string message = $"Words: {wordCount}, Characters: {characterCount}, Characters (no spaces): {characterCountNoSpaces}, Lines: {lineCount}";

            return Task.FromResult(ActionResult.SuccessResult(message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult(
                "Failed to count words and characters",
                ex));
        }
    }
}
