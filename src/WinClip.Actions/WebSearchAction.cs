using Microsoft.Extensions.Configuration;

namespace WinClip.Actions;

/// <summary>
/// Action that searches for selected text using a configurable search engine.
/// </summary>
public class WebSearchAction : IAction
{
    private readonly IBrowserService _browserService;
    private readonly IConfiguration _configuration;

    public WebSearchAction(IBrowserService browserService, IConfiguration configuration)
    {
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string Name => "Web Search";
    public string Description => "Search for selected text using configured search engine";
    public string IconName => "Search";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("Selected text cannot be null or empty"));

            var searchEngineConfig = _configuration["WebSearch:DefaultSearchEngine"];
            var searchEngine = SearchEngineType.Google;

            if (!string.IsNullOrWhiteSpace(searchEngineConfig) &&
                Enum.TryParse<SearchEngineType>(searchEngineConfig, out var configuredEngine))
            {
                searchEngine = configuredEngine;
            }

            var searchUrl = _browserService.BuildSearchUrl(searchEngine, selectedText);
            _browserService.OpenUrl(searchUrl);

            return Task.FromResult(ActionResult.SuccessResult($"Opened {searchEngine} search"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to open web search", ex));
        }
    }
}
