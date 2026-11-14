using System.Net;
using Microsoft.Extensions.Configuration;

namespace WinClip.Actions;

/// <summary>
/// Action that opens Google Translate with the selected text.
/// Target language is configurable via appsettings.json ("Translate:TargetLanguage").
/// </summary>
public class TranslateAction : IAction
{
    private readonly IBrowserService _browserService;
    private readonly IConfiguration _configuration;

    public TranslateAction(IBrowserService browserService, IConfiguration configuration)
    {
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string Name => "Translate";
    public string Description => "Translate selected text using Google Translate";
    public string IconName => "Translate";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected to translate"));

            var targetLang = _configuration["Translate:TargetLanguage"] ?? "en";
            var encoded = WebUtility.UrlEncode(selectedText);

            // Google Translate URL: auto-detect source, translate to target
            var url = $"https://translate.google.com/?sl=auto&tl={targetLang}&text={encoded}&op=translate";
            _browserService.OpenUrl(url);

            return Task.FromResult(ActionResult.SuccessResult($"Opened translation (to {targetLang})"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to open translation", ex));
        }
    }
}
