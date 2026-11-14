namespace WinClip.Actions.AI;

/// <summary>
/// Action that expands selected text with additional details using AI.
/// </summary>
public class ExpandAction : AIActionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpandAction"/> class.
    /// </summary>
    /// <param name="aiService">The AI service to use for text expansion.</param>
    /// <exception cref="ArgumentNullException">Thrown when aiService is null.</exception>
    public ExpandAction(IAIService aiService, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
        : base(aiService, configuration)
    {
    }

    protected override bool IsInPlaceTransform => true;

    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public override string Name => "Expand";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public override string Description => "Expand selected text with additional details using AI";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public override string IconName => "Expand";

    /// <summary>
    /// Processes the selected text using the AI service to generate an expanded version.
    /// </summary>
    /// <param name="selectedText">The text that was selected by the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing the expanded text.</returns>
    protected override async Task<string> ProcessWithAIAsync(string selectedText)
    {
        return await AIService.ExpandAsync(selectedText);
    }
}
