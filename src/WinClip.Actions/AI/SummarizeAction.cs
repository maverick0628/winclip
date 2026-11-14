namespace WinClip.Actions.AI;

/// <summary>
/// Action that summarizes selected text using AI.
/// </summary>
public class SummarizeAction : AIActionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SummarizeAction"/> class.
    /// </summary>
    /// <param name="aiService">The AI service to use for text summarization.</param>
    /// <exception cref="ArgumentNullException">Thrown when aiService is null.</exception>
    public SummarizeAction(IAIService aiService, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
        : base(aiService, configuration)
    {
    }

    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public override string Name => "Summarize";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public override string Description => "Summarize selected text using AI";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public override string IconName => "Summarize";

    /// <summary>
    /// Processes the selected text using the AI service to generate a summary.
    /// </summary>
    /// <param name="selectedText">The text that was selected by the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing the summarized text.</returns>
    protected override async Task<string> ProcessWithAIAsync(string selectedText)
    {
        return await AIService.SummarizeAsync(selectedText);
    }
}
