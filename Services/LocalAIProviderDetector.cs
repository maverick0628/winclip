using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace WinClip.Services;

/// <summary>
/// Detects whether a local AI provider (Ollama, LM Studio) is reachable on its default
/// port. Used at startup to prompt the user to configure WinClip's AI features
/// without manual setup. Pure HTTP probe — no auth, no model load.
/// </summary>
public sealed class LocalAIProviderDetector
{
    public enum Provider { None, Ollama, LMStudio }

    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromMilliseconds(800);
    private readonly ILogger<LocalAIProviderDetector>? _logger;

    public LocalAIProviderDetector(ILogger<LocalAIProviderDetector>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the first local provider that responds successfully.
    /// Order of preference: Ollama first, then LM Studio.
    /// </summary>
    public async Task<Provider> DetectAsync(CancellationToken cancellationToken = default)
    {
        // Ollama: GET /api/tags returns a JSON list of installed models
        if (await ProbeAsync("http://localhost:11434/api/tags", cancellationToken))
        {
            _logger?.LogInformation("Detected local AI provider: Ollama");
            return Provider.Ollama;
        }

        // LM Studio: GET /v1/models returns OpenAI-style model list
        if (await ProbeAsync("http://localhost:1234/v1/models", cancellationToken))
        {
            _logger?.LogInformation("Detected local AI provider: LM Studio");
            return Provider.LMStudio;
        }

        return Provider.None;
    }

    private async Task<bool> ProbeAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var http = new HttpClient { Timeout = ProbeTimeout };
            using var response = await http.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Probe failed for {Url}", url);
            return false;
        }
    }
}
