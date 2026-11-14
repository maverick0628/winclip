namespace WinClip.Actions;

public enum SearchEngineType
{
    Google,
    Bing,
    DuckDuckGo
}

public class SearchEngineConfig
{
    public SearchEngineType Type { get; init; }
    public string Name { get; init; }
    public string UrlTemplate { get; init; }

    public static readonly Dictionary<SearchEngineType, SearchEngineConfig> Defaults = new()
    {
        {
            SearchEngineType.Google,
            new SearchEngineConfig
            {
                Type = SearchEngineType.Google,
                Name = "Google",
                UrlTemplate = "https://www.google.com/search?q={query}"
            }
        },
        {
            SearchEngineType.Bing,
            new SearchEngineConfig
            {
                Type = SearchEngineType.Bing,
                Name = "Bing",
                UrlTemplate = "https://www.bing.com/search?q={query}"
            }
        },
        {
            SearchEngineType.DuckDuckGo,
            new SearchEngineConfig
            {
                Type = SearchEngineType.DuckDuckGo,
                Name = "DuckDuckGo",
                UrlTemplate = "https://duckduckgo.com/?q={query}"
            }
        }
    };

    public static SearchEngineConfig GetConfig(SearchEngineType type) => Defaults[type];

    public string BuildUrl(string query) => UrlTemplate.Replace("{query}", query);

    private SearchEngineConfig()
    {
        Name = string.Empty;
        UrlTemplate = string.Empty;
    }
}
