using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that extracts structured data (emails, URLs, phone numbers, IP addresses)
/// from unstructured text. Implements IContextualAction to only appear when
/// extractable patterns are detected.
/// </summary>
public class SmartExtractAction : IAction, IContextualAction
{
    // Patterns for extraction
    private static readonly Regex EmailPattern = new(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex UrlPattern = new(
        @"https?://[^\s<>""')\]]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhonePattern = new(
        @"(?<!\d)(?:\+?\d{1,3}[\s\-.]?)?\(?\d{3}\)?[\s\-.]?\d{3}[\s\-.]?\d{4}(?!\d)",
        RegexOptions.Compiled);

    private static readonly Regex IpPattern = new(
        @"(?<!\d)(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.(?:25[0-5]|2[0-4]\d|[01]?\d\d?)\.(?:25[0-5]|2[0-4]\d|[01]?\d\d?)(?!\d)",
        RegexOptions.Compiled);

    public string Name => "Smart Extract";
    public string Description => "Extract emails, URLs, phone numbers, and IPs from text";
    public string IconName => "Extract";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show when the text contains at least one extractable pattern.
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;

        return EmailPattern.IsMatch(selectedText) ||
               UrlPattern.IsMatch(selectedText) ||
               PhonePattern.IsMatch(selectedText) ||
               IpPattern.IsMatch(selectedText);
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected to extract from"));

            var results = new List<string>();
            var categories = new List<string>();

            // Extract each type
            var emails = EmailPattern.Matches(selectedText)
                .Select(m => m.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (emails.Count > 0)
            {
                categories.Add($"{emails.Count} email{(emails.Count > 1 ? "s" : "")}");
                results.AddRange(emails);
            }

            var urls = UrlPattern.Matches(selectedText)
                .Select(m => m.Value.TrimEnd('.', ',', ';', ':', ')'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (urls.Count > 0)
            {
                categories.Add($"{urls.Count} URL{(urls.Count > 1 ? "s" : "")}");
                results.AddRange(urls);
            }

            var phones = PhonePattern.Matches(selectedText)
                .Select(m => m.Value)
                .Distinct()
                .ToList();
            if (phones.Count > 0)
            {
                categories.Add($"{phones.Count} phone{(phones.Count > 1 ? "s" : "")}");
                results.AddRange(phones);
            }

            var ips = IpPattern.Matches(selectedText)
                .Select(m => m.Value)
                .Distinct()
                .ToList();
            if (ips.Count > 0)
            {
                categories.Add($"{ips.Count} IP{(ips.Count > 1 ? "s" : "")}");
                results.AddRange(ips);
            }

            if (results.Count == 0)
                return Task.FromResult(ActionResult.FailureResult("No extractable data found"));

            var output = string.Join(Environment.NewLine, results);

            // Retry clipboard access — SetText can fail with COMException when clipboard is locked
            bool clipboardSet = false;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    ClipboardService.Set(output);
                    clipboardSet = true;
                    break;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    Thread.Sleep(50);
                }
            }

            if (!clipboardSet)
                return Task.FromResult(ActionResult.FailureResult("Clipboard is locked — try again"));

            var summary = string.Join(", ", categories);
            return Task.FromResult(ActionResult.SuccessResult($"Extracted {summary} to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to extract data", ex));
        }
    }
}
