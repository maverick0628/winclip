using System.Net;

namespace WinClip.Actions;

/// <summary>
/// Action that looks up the definition of selected text on Dictionary.com.
/// </summary>
public class DictionaryAction : IAction, IContextualAction
{
    private readonly IBrowserService _browserService;

    /// <summary>
    /// Initializes a new instance of the DictionaryAction class.
    /// </summary>
    /// <param name="browserService">Service for browser operations.</param>
    public DictionaryAction(IBrowserService browserService)
    {
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
    }

    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public string Name => "Define";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public string Description => "Look up word definition on Dictionary.com";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public string IconName => "Dictionary";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show this action for short text (single words or short phrases, up to 3 words).
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        var words = selectedText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= 3;
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return Task.FromResult(ActionResult.FailureResult("No text selected to look up"));
            }

            // Build Dictionary.com URL with properly encoded query
            var encodedText = WebUtility.UrlEncode(selectedText.Trim());
            var dictionaryUrl = $"https://www.dictionary.com/browse/{encodedText}";

            // Open the URL in the default browser
            _browserService.OpenUrl(dictionaryUrl);

            return Task.FromResult(ActionResult.SuccessResult($"Looking up definition for '{selectedText}' on Dictionary.com"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult(
                "Failed to open Dictionary.com",
                ex));
        }
    }
}
