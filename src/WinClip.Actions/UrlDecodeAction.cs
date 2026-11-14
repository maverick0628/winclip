using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that URL-decodes the selected text from percent-encoded format.
/// </summary>
public class UrlDecodeAction : IAction, IContextualAction
{
    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public string Name => "URL Decode";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public string Description => "Decode URL-encoded text";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public string IconName => "Link";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show this action if the text contains percent-encoded sequences.
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        // Match %XX hex-encoded sequences (e.g., %20, %3A, %2F)
        return Regex.IsMatch(selectedText, @"%[0-9A-Fa-f]{2}");
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return Task.FromResult(ActionResult.FailureResult("No text selected to decode"));
            }

            // URL decode the text (handles UTF-8 decoding automatically)
            string decodedText = WebUtility.UrlDecode(selectedText);

            // Copy to clipboard (must be called on STA thread)
            ClipboardService.Set(decodedText);

            return Task.FromResult(ActionResult.SuccessResult("Text URL-decoded and copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult(
                "Failed to URL-decode text",
                ex));
        }
    }
}
