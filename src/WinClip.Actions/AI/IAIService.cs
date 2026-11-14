namespace WinClip.Actions.AI;

/// <summary>
/// Defines the contract for AI-powered text processing services.
/// </summary>
public interface IAIService : IDisposable
{
    Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default);
    Task<string> CheckGrammarAsync(string text, CancellationToken cancellationToken = default);
    Task<string> RephraseAsync(string text, CancellationToken cancellationToken = default);
    Task<string> ExpandAsync(string text, CancellationToken cancellationToken = default);
    Task<string> ExecuteCustomPromptAsync(string text, string prompt, CancellationToken cancellationToken = default);
}
