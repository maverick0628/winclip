namespace WinClip.Actions.AI;

/// <summary>
/// Action that processes selected text using a user-defined AI prompt.
/// </summary>
public class CustomPromptAction : AIActionBase
{
    private readonly string _prompt;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomPromptAction"/> class.
    /// </summary>
    /// <param name="aiService">The AI service to use for text processing.</param>
    /// <param name="prompt">The custom prompt to apply to the selected text.</param>
    /// <exception cref="ArgumentNullException">Thrown when aiService or prompt is null.</exception>
    /// <exception cref="ArgumentException">Thrown when prompt is empty or whitespace.</exception>
    public CustomPromptAction(IAIService aiService, string prompt, Microsoft.Extensions.Configuration.IConfiguration? configuration = null)
        : base(aiService, configuration)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or whitespace.", nameof(prompt));
        }

        _prompt = prompt;
    }

    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public override string Name => "Custom Prompt";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public override string Description => "Process selected text with a custom AI prompt";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public override string IconName => "CustomPrompt";

    /// <summary>
    /// Processes the selected text using the AI service with the custom prompt.
    /// </summary>
    /// <param name="selectedText">The text that was selected by the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing the processed text.</returns>
    protected override async Task<string> ProcessWithAIAsync(string selectedText)
    {
        return await AIService.ExecuteCustomPromptAsync(selectedText, _prompt);
    }
}
