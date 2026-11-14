using System.Diagnostics;

namespace WinClip.Actions;

/// <summary>
/// Action that opens Twitter/X compose with the selected text pre-filled.
/// </summary>
public class ShareToTwitterAction : IAction
{
    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public string Name => "Share to Twitter/X";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public string Description => "Share selected text on Twitter/X";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public string IconName => "Twitter";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return Task.FromResult(ActionResult.FailureResult("No text selected to share"));
            }

            // Truncate to 280 characters with ellipsis if needed
            string tweetText = selectedText.Length > 280
                ? selectedText[..277] + "..."
                : selectedText;

            // URL encode the text
            string encodedText = Uri.EscapeDataString(tweetText);

            // Build Twitter compose URL
            string twitterUrl = $"https://twitter.com/intent/tweet?text={encodedText}";

            // Open URL in default browser with local safety validation.
            if (!Uri.TryCreate(twitterUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return Task.FromResult(ActionResult.FailureResult("Generated Twitter URL is invalid"));
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });

            return Task.FromResult(ActionResult.SuccessResult("Opened Twitter compose"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult(
                "Failed to open Twitter compose",
                ex));
        }
    }
}
