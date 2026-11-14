using System.Net;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that URL-encodes the selected text for safe usage in URLs.
/// </summary>
public class UrlEncodeAction : IAction
{
    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public string Name => "URL Encode";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public string Description => "Encode selected text for URL usage";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public string IconName => "Link";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return Task.FromResult(ActionResult.FailureResult("No text selected to encode"));
            }

            // URL encode the text (handles UTF-8 encoding automatically)
            string encodedText = WebUtility.UrlEncode(selectedText);

            // Copy to clipboard (must be called on STA thread)
            ClipboardService.Set(encodedText);

            return Task.FromResult(ActionResult.SuccessResult("Text URL-encoded and copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult(
                "Failed to URL-encode text",
                ex));
        }
    }
}
