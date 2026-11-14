using System.Diagnostics;
using System.Net;

namespace WinClip.Actions;

/// <summary>
/// Service for handling browser operations including URL opening and search URL building.
/// Validates URLs to prevent command injection via UseShellExecute.
/// </summary>
public class BrowserService : IBrowserService
{
    public void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url), "URL cannot be null or empty");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "URL must be a valid absolute http or https URL", nameof(url));
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = uri.AbsoluteUri,
            UseShellExecute = true
        });
    }

    public string BuildSearchUrl(SearchEngineType engine, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query), "Query cannot be null or empty");

        var config = SearchEngineConfig.GetConfig(engine);
        return config.BuildUrl(WebUtility.UrlEncode(query));
    }
}
