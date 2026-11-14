using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace WinClip.Actions.AI;

/// <summary>
/// Service for interacting with OpenAI's API for text processing operations.
/// </summary>
public class OpenAIService : AIServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private const string OpenAIApiUrl = "https://api.openai.com/v1/chat/completions";

    public OpenAIService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public override void Dispose() => _httpClient.Dispose();

    protected override async Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["AI:OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenAI API key is not configured. Please set AI:OpenAI:ApiKey in appsettings.json");

            // Default to gpt-4o — modern, fast, widely available. Users can override
            // via AI:OpenAI:Model in appsettings.json.
            var model = _configuration["AI:OpenAI:Model"] ?? "gpt-4o";
            var maxTokensStr = _configuration["AI:OpenAI:MaxTokens"] ?? "2000";
            var temperatureStr = _configuration["AI:OpenAI:Temperature"] ?? "0.7";

            if (!int.TryParse(maxTokensStr, out var maxTokens)) maxTokens = 2000;
            if (!double.TryParse(temperatureStr, out var temperature)) temperature = 0.7;

            var request = new OpenAIRequest
            {
                Model = model,
                Messages = new[]
                {
                    new OpenAIMessage { Role = "system", Content = systemPrompt },
                    new OpenAIMessage { Role = "user", Content = userPrompt }
                },
                MaxTokens = maxTokens,
                Temperature = temperature
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAIApiUrl);
            httpRequest.Content = JsonContent.Create(request);
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"OpenAI API request failed with status {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(cancellationToken: cancellationToken);

            if (result?.Choices == null || result.Choices.Length == 0)
                throw new InvalidOperationException("OpenAI API returned an empty response");

            var content = result.Choices[0].Message.Content;
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("AI service returned empty content");
            return content.Trim();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Failed to connect to OpenAI API. Please check your internet connection.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse OpenAI API response", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not OperationCanceledException)
        {
            throw new InvalidOperationException("An error occurred while communicating with OpenAI API", ex);
        }
    }

    #region Request/Response Models

    private class OpenAIRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public OpenAIMessage[] Messages { get; set; } = Array.Empty<OpenAIMessage>();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public OpenAIChoice[] Choices { get; set; } = Array.Empty<OpenAIChoice>();
    }

    private class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage Message { get; set; } = new();
    }

    #endregion
}
