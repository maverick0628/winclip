using System.Net;

namespace WinClip.Actions;

/// <summary>
/// Action that opens a QR code generator with the selected text.
/// Uses a free online QR code API to display the code in the default browser.
/// </summary>
public class QrCodeAction : IAction
{
    private readonly IBrowserService _browserService;

    public QrCodeAction(IBrowserService browserService)
    {
        _browserService = browserService ?? throw new ArgumentNullException(nameof(browserService));
    }

    public string Name => "QR Code";
    public string Description => "Generate a QR code from selected text";
    public string IconName => "QrCode";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            // Limit to reasonable QR code size (max ~4,296 alphanumeric chars)
            if (selectedText.Length > 2000)
                return Task.FromResult(ActionResult.FailureResult("Text too long for QR code (max 2,000 characters)"));

            var encoded = WebUtility.UrlEncode(selectedText);

            // Use qrserver.com free API — returns an image directly
            var url = $"https://api.qrserver.com/v1/create-qr-code/?size=400x400&data={encoded}";
            _browserService.OpenUrl(url);

            return Task.FromResult(ActionResult.SuccessResult("QR code opened in browser"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to generate QR code", ex));
        }
    }
}
