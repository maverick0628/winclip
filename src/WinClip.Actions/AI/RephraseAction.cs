namespace WinClip.Actions.AI;

/// <summary>
/// Action that rephrases selected text using AI.
/// </summary>
public class RephraseAction : AIActionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RephraseAction"/> class.
    /// </summary>
    /// <param name="aiService">The AI service to use for text rephrasing.</param>
    /// <exception cref="ArgumentNullException">Thrown when aiService is null.</exception>
    public RephraseAction(IAIService aiService, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
        : base(aiService, configuration)
    {
    }

    protected override bool IsInPlaceTransform => true;

    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public override string Name => "Rephrase";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public override string Description => "Rephrase selected text using AI";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public override string IconName => "Rephrase";

    /// <summary>
    /// Processes the selected text using the AI service to generate a rephrased version.
    /// </summary>
    /// <param name="selectedText">The text that was selected by the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing the rephrased text.</returns>
    protected override async Task<string> ProcessWithAIAsync(string selectedText)
    {
        return await AIService.RephraseAsync(selectedText);
    }
}
