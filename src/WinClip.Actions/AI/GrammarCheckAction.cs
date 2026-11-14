namespace WinClip.Actions.AI;

/// <summary>
/// Action that checks grammar and style of selected text using AI.
/// </summary>
public class GrammarCheckAction : AIActionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GrammarCheckAction"/> class.
    /// </summary>
    /// <param name="aiService">The AI service to use for grammar checking.</param>
    /// <exception cref="ArgumentNullException">Thrown when aiService is null.</exception>
    public GrammarCheckAction(IAIService aiService, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
        : base(aiService, configuration)
    {
    }

    protected override bool IsInPlaceTransform => true;

    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public override string Name => "Check Grammar";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public override string Description => "Check grammar and style of selected text using AI";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public override string IconName => "Grammar";

    /// <summary>
    /// Processes the selected text using the AI service to check grammar and style.
    /// </summary>
    /// <param name="selectedText">The text that was selected by the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing the grammar-checked text.</returns>
    protected override async Task<string> ProcessWithAIAsync(string selectedText)
    {
        return await AIService.CheckGrammarAsync(selectedText);
    }
}
