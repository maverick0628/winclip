using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace WinClip.Actions.AI;

/// <summary>
/// Service for interacting with Anthropic's Claude API for text processing operations.
/// </summary>
public class ClaudeService : AIServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private const string ClaudeApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    public ClaudeService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public override void Dispose() => _httpClient.Dispose();

    protected override async Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["AI:Claude:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Claude API key is not configured. Please set AI:Claude:ApiKey in appsettings.json");

            // Default to the current Sonnet generation. Older 3.x model IDs return 404
            // from the API. Users can override via AI:Claude:Model in appsettings.json
            // (e.g. "claude-haiku-4-5-20251001" for cheaper/faster responses).
            var model = _configuration["AI:Claude:Model"] ?? "claude-sonnet-4-6";
            var maxTokensStr = _configuration["AI:Claude:MaxTokens"] ?? "2000";
            var temperatureStr = _configuration["AI:Claude:Temperature"] ?? "0.7";

            if (!int.TryParse(maxTokensStr, out var maxTokens)) maxTokens = 2000;
            if (!double.TryParse(temperatureStr, out var temperature)) temperature = 0.7;

            var request = new ClaudeRequest
            {
                Model = model,
                System = systemPrompt,
                Messages = new[]
                {
                    new ClaudeMessage { Role = "user", Content = userPrompt }
                },
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ClaudeApiUrl);
            httpRequest.Content = JsonContent.Create(request);
            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", AnthropicVersion);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Claude API request failed with status {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<ClaudeResponse>(cancellationToken: cancellationToken);

            if (result?.Content == null || result.Content.Length == 0)
                throw new InvalidOperationException("Claude API returned an empty response");

            var content = result.Content[0].Text;
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("AI service returned empty content");
            return content.Trim();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Failed to connect to Claude API. Please check your internet connection.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse Claude API response", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not OperationCanceledException)
        {
            throw new InvalidOperationException("An error occurred while communicating with Claude API", ex);
        }
    }

    #region Request/Response Models

    private class ClaudeRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("system")]
        public string System { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public ClaudeMessage[] Messages { get; set; } = Array.Empty<ClaudeMessage>();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private class ClaudeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class ClaudeResponse
    {
        [JsonPropertyName("content")]
        public ClaudeContent[] Content { get; set; } = Array.Empty<ClaudeContent>();
    }

    private class ClaudeContent
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    #endregion
}
