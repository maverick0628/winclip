using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace WinClip.Services;

/// <summary>
/// Strictly opt-in usage telemetry. When enabled in settings, sends a single
/// fire-and-forget HTTP POST per action invocation to Plausible Analytics.
///
/// Privacy guarantees:
/// - Never sends user content (selected text, clipboard contents, results)
/// - No user identifier — Plausible doesn't use cookies or fingerprints
/// - Only the action name and app version are transmitted
/// - No network call at all when <c>EnableUsageTelemetry</c> is false (default)
/// - Failures are silent: telemetry never blocks UI or surfaces errors to the user
/// </summary>
public sealed class TelemetryClient : IDisposable
{
    private const string DefaultDomain = "winclip.duncansmith.tech";
    private const string PlausibleEndpoint = "https://plausible.io/api/event";

    private readonly HttpClient _httpClient;
    private readonly SettingsManager? _settings;
    private readonly ILogger<TelemetryClient>? _logger;

    public TelemetryClient(SettingsManager? settings = null, ILogger<TelemetryClient>? logger = null)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        // Plausible filters requests with no User-Agent as bots.
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"WinClip/{AppInfo.DisplayVersion} (Windows)");
    }

    public void Dispose() => _httpClient.Dispose();

    /// <summary>
    /// Records an action invocation. No-op if telemetry is disabled in settings.
    /// Always returns immediately — the network call runs in the background.
    /// </summary>
    public void TrackActionInvoked(string actionName)
    {
        if (_settings == null || !_settings.EnableUsageTelemetry) return;
        if (string.IsNullOrWhiteSpace(actionName)) return;

        var domain = !string.IsNullOrWhiteSpace(_settings.TelemetryDomain)
            ? _settings.TelemetryDomain!
            : DefaultDomain;

        // Fire-and-forget; capture only what we need to avoid holding settings ref.
        var version = AppInfo.DisplayVersion;
        _ = SendAsync(domain, actionName, version);
    }

    private async Task SendAsync(string domain, string actionName, string version)
    {
        try
        {
            var payload = new PlausibleEvent
            {
                Name = "action_invoked",
                Url = $"app://winclip/action/{Uri.EscapeDataString(actionName)}",
                Domain = domain,
                Props = new Dictionary<string, string>
                {
                    ["action"] = actionName,
                    ["version"] = version
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, PlausibleEndpoint)
            {
                Content = JsonContent.Create(payload)
            };
            // Plausible requires this header to attribute the event correctly to a domain.
            request.Headers.Add("X-Forwarded-For", "127.0.0.1");

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogDebug("Plausible event POST returned {Status} for action {Action}",
                    response.StatusCode, actionName);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Telemetry post failed for action {Action} (suppressed)", actionName);
        }
    }

    private sealed class PlausibleEvent
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
        [JsonPropertyName("domain")] public string Domain { get; set; } = string.Empty;
        [JsonPropertyName("props")] public Dictionary<string, string> Props { get; set; } = new();
    }
}
