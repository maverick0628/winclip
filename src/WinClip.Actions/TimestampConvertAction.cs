using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that converts between Unix timestamps and human-readable dates.
/// Contextual: only appears when text looks like a Unix epoch (10 or 13 digits)
/// or an ISO 8601 / common date format.
/// </summary>
public class TimestampConvertAction : IAction, IContextualAction
{
    // 10-digit Unix seconds or 13-digit Unix milliseconds
    private static readonly Regex UnixPattern = new(
        @"^\s*(\d{10}|\d{13})\s*$",
        RegexOptions.Compiled);

    public string Name => "Timestamp Convert";
    public string Description => "Convert between Unix timestamps and readable dates";
    public string IconName => "Clock";

    public bool IsAvailable() => true;

    /// <summary>
    /// Show when text is a Unix epoch (10 or 13 digits) or a parseable date string.
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        var trimmed = selectedText.Trim();

        // Unix epoch?
        if (UnixPattern.IsMatch(trimmed)) return true;

        // Parseable date?
        return DateTimeOffset.TryParse(trimmed, out _);
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            var trimmed = selectedText.Trim();

            // Try Unix epoch first
            if (UnixPattern.IsMatch(trimmed) && long.TryParse(trimmed, out var epoch))
            {
                DateTimeOffset dto;
                string label;

                if (trimmed.Length == 13)
                {
                    // Milliseconds
                    dto = DateTimeOffset.FromUnixTimeMilliseconds(epoch);
                    label = "ms";
                }
                else
                {
                    // Seconds
                    dto = DateTimeOffset.FromUnixTimeSeconds(epoch);
                    label = "s";
                }

                var readable = dto.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz");
                ClipboardService.Set(readable);

                return Task.FromResult(ActionResult.SuccessResult($"Epoch ({label}) \u2192 {readable}"));
            }

            // Try parsing as a date string → convert to Unix epoch
            if (DateTimeOffset.TryParse(trimmed, out var parsed))
            {
                var unixSeconds = parsed.ToUnixTimeSeconds();
                var result = unixSeconds.ToString();
                ClipboardService.Set(result);

                return Task.FromResult(ActionResult.SuccessResult($"Date \u2192 {result} (Unix seconds)"));
            }

            return Task.FromResult(ActionResult.FailureResult("Could not parse as timestamp or date"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to convert timestamp", ex));
        }
    }
}
