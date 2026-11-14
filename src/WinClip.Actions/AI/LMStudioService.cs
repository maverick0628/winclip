using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace WinClip.Actions.AI;

/// <summary>
/// Service for interacting with LM Studio's local OpenAI-compatible API.
/// Default endpoint: http://localhost:1234/v1/chat/completions
/// </summary>
public class LMStudioService : AIServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public LMStudioService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        // Local models on consumer hardware can take 30-90s for first token (JIT model
        // load + cold cache + reasoning models). Default to 180s, configurable via
        // AI:LMStudio:TimeoutSeconds.
        var timeoutStr = _configuration["AI:LMStudio:TimeoutSeconds"] ?? "180";
        if (!int.TryParse(timeoutStr, out var timeoutSeconds) || timeoutSeconds <= 0)
            timeoutSeconds = 180;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public override void Dispose() => _httpClient.Dispose();

    protected override async Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["AI:LMStudio:Endpoint"] ?? "http://localhost:1234";
            // Normalize: tolerate users entering the endpoint with or without a trailing
            // /v1 (the AISetup docs historically suggested /v1, but the code appends it).
            var normalized = baseUrl.TrimEnd('/');
            if (normalized.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                normalized = normalized[..^3];
            var apiUrl = $"{normalized}/v1/chat/completions";
            var model = _configuration["AI:LMStudio:Model"] ?? "default";
            var maxTokensStr = _configuration["AI:LMStudio:MaxTokens"] ?? "2000";
            var temperatureStr = _configuration["AI:LMStudio:Temperature"] ?? "0.7";

            if (!int.TryParse(maxTokensStr, out var maxTokens)) maxTokens = 2000;
            if (!double.TryParse(temperatureStr, out var temperature)) temperature = 0.7;

            var request = new LMStudioRequest
            {
                Model = model,
                Messages = new[]
                {
                    new LMStudioMessage { Role = "system", Content = systemPrompt },
                    new LMStudioMessage { Role = "user", Content = userPrompt }
                },
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            httpRequest.Content = JsonContent.Create(request);

            var apiKey = _configuration["AI:LMStudio:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // HttpClient timeout — surface as actionable error rather than "Cancelled"
                throw new InvalidOperationException(
                    $"LM Studio request timed out after {_httpClient.Timeout.TotalSeconds:0}s. " +
                    "Try a faster (non-reasoning) model, or increase AI:LMStudio:TimeoutSeconds in appsettings.json.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"LM Studio API request failed with status {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<LMStudioResponse>(cancellationToken: cancellationToken);

            if (result?.Choices == null || result.Choices.Length == 0)
                throw new InvalidOperationException("LM Studio returned an empty response");

            var content = result.Choices[0].Message.Content;
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("AI service returned empty content");
            return content.Trim();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                "Failed to connect to LM Studio. Make sure LM Studio is running and the local server is started.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse LM Studio response", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not OperationCanceledException)
        {
            throw new InvalidOperationException("An error occurred while communicating with LM Studio", ex);
        }
    }

    #region Request/Response Models (OpenAI-compatible format)

    private class LMStudioRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public LMStudioMessage[] Messages { get; set; } = Array.Empty<LMStudioMessage>();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private class LMStudioMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class LMStudioResponse
    {
        [JsonPropertyName("choices")]
        public LMStudioChoice[] Choices { get; set; } = Array.Empty<LMStudioChoice>();
    }

    private class LMStudioChoice
    {
        [JsonPropertyName("message")]
        public LMStudioMessage Message { get; set; } = new();
    }

    #endregion
}
