using Microsoft.Extensions.Configuration;

namespace WinClip.Actions.AI;

/// <summary>
/// Abstract base class for AI-powered actions.
/// Provides common functionality for actions that use AI services to process text.
/// </summary>
public abstract class AIActionBase : IAction
{
    /// <summary>
    /// Gets the AI service used to process text.
    /// </summary>
    protected IAIService AIService { get; }

    private readonly IConfiguration? _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIActionBase"/> class.
    /// </summary>
    /// <param name="aiService">The AI service to use for text processing.</param>
    /// <param name="configuration">Optional configuration for checking API key availability.</param>
    /// <exception cref="ArgumentNullException">Thrown when aiService is null.</exception>
    protected AIActionBase(IAIService aiService, IConfiguration? configuration = null)
    {
        AIService = aiService ?? throw new ArgumentNullException(nameof(aiService));
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public abstract string IconName { get; }

    /// <summary>
    /// Whether the action produces an in-place text replacement (auto-paste).
    /// Override to return true for actions like Grammar Check, Rephrase, Expand.
    /// </summary>
    protected virtual bool IsInPlaceTransform => false;

    /// <summary>
    /// AI actions are always visible in the popup so users can discover them.
    /// If no AI provider is configured, ExecuteAsync shows a configuration hint.
    /// </summary>
    public bool IsAvailable() => true;

    /// <summary>
    /// Returns true when at least one AI provider has credentials configured.
    /// </summary>
    private bool IsAIConfigured()
    {
        if (_configuration == null) return true;
        return !string.IsNullOrWhiteSpace(_configuration["AI:OpenAI:ApiKey"])
            || !string.IsNullOrWhiteSpace(_configuration["AI:Claude:ApiKey"])
            || !string.IsNullOrWhiteSpace(_configuration["AI:LMStudio:Endpoint"]);
    }

    public async Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return ActionResult.FailureResult("No text selected to process");
            }

            // Check AI provider is configured — guide user if not
            if (!IsAIConfigured())
            {
                return ActionResult.FailureResult("Set up AI in Settings \u2192 AI Features");
            }

            // Execute the AI processing
            var result = await ProcessWithAIAsync(selectedText);

            // Validate result
            if (string.IsNullOrWhiteSpace(result))
            {
                return ActionResult.FailureResult("AI service returned empty result");
            }

            // Try to put AI result on clipboard. If a clipboard manager (PowerToys
            // Advanced Paste, Ditto, etc.) is holding the lock past our 3s grace,
            // *don't lose the AI work*. Return the result text in the message so the
            // host UI can display it for manual copy via the preview window.
            var clipboardOk = ClipboardService.TrySet(result);
            if (!clipboardOk)
            {
                return new ActionResult(
                    success: true,
                    message: $"AI_RESULT_NO_CLIPBOARD::{result}",
                    autoPaste: false);
            }

            return IsInPlaceTransform
                ? ActionResult.SuccessWithPaste($"{Name} completed")
                : ActionResult.SuccessResult($"{Name} — copied to clipboard");
        }
        catch (OperationCanceledException)
        {
            return ActionResult.FailureResult("Cancelled");
        }
        catch (ArgumentException ex)
        {
            // Both ArgumentException and ArgumentNullException — invalid input from caller
            return ActionResult.FailureResult($"{Name}: {Trim(ex.Message)}", ex);
        }
        catch (InvalidOperationException ex)
        {
            // ClaudeService / OpenAIService / LMStudioService raise this for every
            // HTTP failure, model-not-found, empty content, etc. The message contains
            // the actual cause (e.g. "Claude API request failed with status 401: ...").
            // Pre-v2.1 this was masked as "AI service is not configured or unavailable"
            // and users had no way to diagnose. Surface the real message instead.
            return ActionResult.FailureResult($"{Name}: {Trim(ex.Message)}", ex);
        }
        catch (Exception ex)
        {
            return ActionResult.FailureResult($"{Name}: {Trim(ex.Message)}", ex);
        }
    }

    /// <summary>Trim an error message so the popup doesn't overflow on long API responses.</summary>
    private static string Trim(string msg) =>
        string.IsNullOrEmpty(msg) ? "unknown error"
        : msg.Length <= 140 ? msg : msg[..137] + "...";

    /// <summary>
    /// Processes the selected text using the AI service.
    /// Derived classes must implement this method to define their specific AI processing logic.
    /// </summary>
    /// <param name="selectedText">The text that was selected by the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing the processed text.</returns>
    protected abstract Task<string> ProcessWithAIAsync(string selectedText);
}
