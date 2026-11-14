using System.Text;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that Base64-encodes the selected text.
/// </summary>
public class Base64EncodeAction : IAction
{
    public string Name => "Base64 Encode";
    public string Description => "Encode selected text as Base64";
    public string IconName => "Lock";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected to encode"));

            var bytes = Encoding.UTF8.GetBytes(selectedText);
            var encoded = Convert.ToBase64String(bytes);

            ClipboardService.Set(encoded);

            return Task.FromResult(ActionResult.SuccessResult("Base64-encoded and copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to Base64-encode text", ex));
        }
    }
}
