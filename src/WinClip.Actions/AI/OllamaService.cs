using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace WinClip.Actions.AI;

/// <summary>
/// Service for interacting with a local Ollama server.
/// Default endpoint: http://localhost:11434/api/chat
/// API shape differs from OpenAI: a single message object in the response (no choices array)
/// and an options block for tuning parameters.
/// </summary>
public class OllamaService : AIServiceBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OllamaService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        var timeoutStr = _configuration["AI:Ollama:TimeoutSeconds"] ?? "180";
        if (!int.TryParse(timeoutStr, out var timeoutSeconds) || timeoutSeconds <= 0)
            timeoutSeconds = 180;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
    }

    public override void Dispose() => _httpClient.Dispose();

    protected override async Task<string> ExecutePromptAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["AI:Ollama:Endpoint"] ?? "http://localhost:11434";
            var apiUrl = $"{baseUrl.TrimEnd('/')}/api/chat";
            var model = _configuration["AI:Ollama:Model"] ?? "llama3.2";
            var maxTokensStr = _configuration["AI:Ollama:MaxTokens"] ?? "2000";
            var temperatureStr = _configuration["AI:Ollama:Temperature"] ?? "0.7";

            if (!int.TryParse(maxTokensStr, out var maxTokens)) maxTokens = 2000;
            if (!double.TryParse(temperatureStr, out var temperature)) temperature = 0.7;

            var request = new OllamaRequest
            {
                Model = model,
                Messages = new[]
                {
                    new OllamaMessage { Role = "system", Content = systemPrompt },
                    new OllamaMessage { Role = "user", Content = userPrompt }
                },
                Stream = false,
                Options = new OllamaOptions
                {
                    NumPredict = maxTokens,
                    Temperature = temperature
                }
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            httpRequest.Content = JsonContent.Create(request);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException(
                    $"Ollama request timed out after {_httpClient.Timeout.TotalSeconds:0}s. " +
                    "Try a smaller model, or increase AI:Ollama:TimeoutSeconds in appsettings.json.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Ollama API request failed with status {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken);

            var content = result?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Ollama returned empty content");
            return content.Trim();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                "Failed to connect to Ollama. Make sure Ollama is running (`ollama serve`) and the configured model is pulled (`ollama pull <model>`).", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse Ollama response", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not OperationCanceledException)
        {
            throw new InvalidOperationException("An error occurred while communicating with Ollama", ex);
        }
    }

    #region Request/Response Models

    private class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public OllamaMessage[] Messages { get; set; } = Array.Empty<OllamaMessage>();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("options")]
        public OllamaOptions Options { get; set; } = new();
    }

    private class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class OllamaOptions
    {
        [JsonPropertyName("num_predict")]
        public int NumPredict { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    private class OllamaResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }

    #endregion
}
