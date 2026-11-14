using WinClip.Actions;

namespace WinClip.Integrations.Blip;

/// <summary>
/// Action that sends selected text to the Blip note-taking application.
/// Appears in the WinClip popup menu when Blip is installed.
/// </summary>
public class BlipAction : IAction
{
    private readonly IBlipDetector _detector;
    private readonly IBlipCommunicator _communicator;
    private readonly BlipActionSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlipAction"/> class.
    /// </summary>
    /// <param name="detector">The Blip detector for checking installation status.</param>
    /// <param name="communicator">The communicator for sending notes to Blip.</param>
    /// <param name="settings">The settings controlling what metadata to include in notes.</param>
    public BlipAction(IBlipDetector detector, IBlipCommunicator communicator, BlipActionSettings? settings = null)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
        _settings = settings ?? new BlipActionSettings();
    }

    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public string Name => "Send to Blip";

    /// <summary>
    /// Gets the icon identifier for the action.
    /// This should reference the Blip logo icon resource.
    /// </summary>
    public string IconName => "blip-logo";

    /// <summary>
    /// Gets the description of the action shown in tooltips.
    /// </summary>
    public string Description => "Send selected text to Blip note-taking app";

    /// <summary>
    /// Determines whether the action is available.
    /// The action is only available when Blip is installed.
    /// </summary>
    /// <returns>True if Blip is installed, false otherwise.</returns>
    public bool IsAvailable()
    {
        return _detector.IsInstalled();
    }

    /// <summary>
    /// Executes the action by sending the selected text to Blip.
    /// If Blip is not running, it will be launched automatically.
    /// </summary>
    /// <param name="selectedText">The text selected by the user.</param>
    /// <param name="context">Optional context information about where the text was selected from.</param>
    /// <returns>The result of the action execution.</returns>
    public async Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        if (string.IsNullOrWhiteSpace(selectedText))
            return ActionResult.FailureResult("No text selected");

        if (!_detector.IsInstalled())
            return ActionResult.FailureResult("Blip is not installed");

        var request = new BlipNoteRequest
        {
            Text = selectedText,
            SourceApp = _settings.IncludeSourceApp && context != null
                ? context.SourceApp
                : null,
            SourceUrl = _settings.IncludeSourceUrl && context != null
                ? context.SourceUrl
                : null
        };

        try
        {
            // Try sending via named pipe first (ideal path — zero-click handoff)
            var success = await _communicator.SendNoteAsync(request);
            if (success)
                return ActionResult.SuccessResult("Note sent to Blip");

            // Pipe not available — launch Blip with text on clipboard (user pastes)
            // Ensure text is on clipboard (should already be there from selection)
            try { System.Windows.Clipboard.SetText(selectedText); }
            catch { /* Clipboard already has the text from selection */ }

            var launched = await _communicator.LaunchBlipAsync();
            if (launched)
            {
                // Try pipe one more time now that Blip is running
                success = await _communicator.SendNoteAsync(request);
                if (success)
                    return ActionResult.SuccessResult("Note sent to Blip");
            }

            // Fallback: Blip is open (or opening), text is on clipboard
            return ActionResult.SuccessResult("Blip opened — paste with Ctrl+V");
        }
        catch (Exception ex)
        {
            return ActionResult.FailureResult("Error sending note to Blip", ex);
        }
    }
}
