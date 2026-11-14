using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that strips HTML tags from selected text, leaving clean plain text.
/// Contextual: only appears when text contains HTML-like tags.
/// </summary>
public class HtmlStripAction : IAction, IContextualAction
{
    private static readonly Regex TagPattern = new(
        @"<[a-zA-Z/!][^>]*>",
        RegexOptions.Compiled);

    private static readonly Regex AllTagsPattern = new(
        @"<[^>]+>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex WhitespaceCollapsePattern = new(
        @"[ \t]+",
        RegexOptions.Compiled);

    private static readonly Regex BlankLinesPattern = new(
        @"(\r?\n){3,}",
        RegexOptions.Compiled);

    public string Name => "Strip HTML";
    public string Description => "Remove HTML tags and decode entities to plain text";
    public string IconName => "Code";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show when text contains HTML-like tags.
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        return TagPattern.IsMatch(selectedText);
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            // Insert line breaks for block-level elements before stripping
            var text = Regex.Replace(selectedText, @"<\s*(br|hr)\s*/?\s*>", "\n",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<\s*/?\s*(p|div|h[1-6]|li|tr|blockquote|pre)\b[^>]*>", "\n",
                RegexOptions.IgnoreCase);

            // Strip all remaining tags
            text = AllTagsPattern.Replace(text, "");

            // Decode HTML entities (&amp; → &, &lt; → <, &#39; → ', etc.)
            text = WebUtility.HtmlDecode(text);

            // Clean up excessive whitespace
            text = WhitespaceCollapsePattern.Replace(text, " ");
            text = BlankLinesPattern.Replace(text, "\n\n");
            text = text.Trim();

            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(ActionResult.FailureResult("No text remained after stripping HTML"));

            ClipboardService.Set(text);

            return Task.FromResult(ActionResult.SuccessResult("HTML stripped — plain text copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to strip HTML", ex));
        }
    }
}
