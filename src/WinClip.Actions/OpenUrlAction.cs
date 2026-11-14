using System.Diagnostics;

namespace WinClip.Actions;

/// <summary>
/// Action that opens a URL in the default browser.
/// </summary>
public class OpenUrlAction : IAction, IContextualAction
{
    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public string Name => "Open URL";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public string Description => "Open URL in default browser";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public string IconName => "Globe";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show this action if the selected text looks like a URL.
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        var trimmed = selectedText.Trim();

        // Obvious protocol prefix
        if (trimmed.Contains("://")) return true;

        // Starts with www.
        if (trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) return true;

        // Looks like a domain (e.g., "example.com", "foo.co.uk")
        if (!trimmed.Contains(' ') && trimmed.Contains('.'))
        {
            var candidate = "https://" + trimmed;
            if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
                uri.Host.Contains('.'))
                return true;
        }

        return false;
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return Task.FromResult(ActionResult.FailureResult("No text selected to open"));
            }

            // Normalize the URL by adding https:// if no protocol is specified
            string url = selectedText.Trim();

            // First try parsing as-is to detect existing schemes (mailto:, ftp:, etc.)
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? validatedUri))
            {
                // Not a valid absolute URI — try with https:// prefix
                url = "https://" + url;
                if (!Uri.TryCreate(url, UriKind.Absolute, out validatedUri))
                {
                    return Task.FromResult(ActionResult.FailureResult("Selected text is not a valid URL"));
                }
            }

            // Ensure the URI has a supported scheme (http or https)
            if (validatedUri.Scheme != Uri.UriSchemeHttp && validatedUri.Scheme != Uri.UriSchemeHttps)
            {
                return Task.FromResult(ActionResult.FailureResult("URL must use http or https protocol"));
            }

            // Open URL in default browser
            var processStartInfo = new ProcessStartInfo
            {
                FileName = validatedUri.AbsoluteUri,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);

            return Task.FromResult(ActionResult.SuccessResult($"Opened {validatedUri.AbsoluteUri}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult(
                "Failed to open URL",
                ex));
        }
    }
}
