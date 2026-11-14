using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that Base64-decodes the selected text.
/// Implements IContextualAction to only appear when text looks like Base64.
/// </summary>
public class Base64DecodeAction : IAction, IContextualAction
{
    public string Name => "Base64 Decode";
    public string Description => "Decode Base64-encoded text";
    public string IconName => "Lock";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show when text looks like Base64: A-Z, a-z, 0-9, +, /, =
    /// with minimum length and valid padding.
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;

        var trimmed = selectedText.Trim();

        // Must be at least 4 chars and consist only of Base64 characters
        if (trimmed.Length < 4) return false;
        if (!Regex.IsMatch(trimmed, @"^[A-Za-z0-9+/\r\n]+=*$")) return false;

        // Length (excluding whitespace) must be a multiple of 4
        var stripped = Regex.Replace(trimmed, @"\s+", "");
        return stripped.Length % 4 == 0;
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected to decode"));

            var trimmed = selectedText.Trim();
            var bytes = Convert.FromBase64String(trimmed);
            var decoded = Encoding.UTF8.GetString(bytes);

            ClipboardService.Set(decoded);

            return Task.FromResult(ActionResult.SuccessResult("Base64-decoded and copied to clipboard"));
        }
        catch (FormatException)
        {
            return Task.FromResult(ActionResult.FailureResult("Invalid Base64 — could not decode"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to Base64-decode text", ex));
        }
    }
}
