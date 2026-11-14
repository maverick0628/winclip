using System.Diagnostics;

namespace WinClip.Actions;

/// <summary>
/// Action that composes a new email with the selected text in the body.
/// Uses mailto: protocol to open the default email client.
/// </summary>
public class EmailAction : IAction
{
    private readonly IProcessLauncher _processLauncher;

    /// <summary>
    /// Initializes a new instance using the real <see cref="ProcessLauncher"/>.
    /// This parameterless constructor is what production registration uses.
    /// </summary>
    public EmailAction() : this(new ProcessLauncher())
    {
    }

    /// <summary>
    /// Initializes a new instance with an explicit <see cref="IProcessLauncher"/>.
    /// Used by tests to verify launch behaviour without starting a real process.
    /// </summary>
    public EmailAction(IProcessLauncher processLauncher)
    {
        _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
    }

    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public string Name => "Email";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public string Description => "Compose email with selected text";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public string IconName => "Email";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return Task.FromResult(ActionResult.FailureResult("No text selected to email"));
            }

            // URL encode the text for mailto: protocol
            // Uri.EscapeDataString handles special characters and preserves line breaks
            var encodedBody = Uri.EscapeDataString(selectedText);

            // Create mailto: URL with body parameter
            var mailtoUrl = $"mailto:?body={encodedBody}";

            // Open default email client
            var processStartInfo = new ProcessStartInfo
            {
                FileName = mailtoUrl,
                UseShellExecute = true
            };

            _processLauncher.Start(processStartInfo);

            return Task.FromResult(ActionResult.SuccessResult("Email client opened"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult(
                "Failed to open email client",
                ex));
        }
    }
}
