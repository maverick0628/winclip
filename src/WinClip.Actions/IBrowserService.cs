namespace WinClip.Actions;

/// <summary>
/// Service for handling browser operations.
/// </summary>
public interface IBrowserService
{
    /// <summary>
    /// Opens the specified URL in the default browser.
    /// </summary>
    void OpenUrl(string url);

    /// <summary>
    /// Builds a search URL for the specified search engine and query.
    /// </summary>
    string BuildSearchUrl(SearchEngineType engine, string query);
}
