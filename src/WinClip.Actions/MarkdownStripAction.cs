using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that strips Markdown formatting to produce clean plain text.
/// Contextual: only appears when text contains Markdown syntax.
/// Handles: headers, bold, italic, links, images, code blocks, lists, blockquotes,
/// horizontal rules, strikethrough, and HTML tags.
/// </summary>
public class MarkdownStripAction : IAction, IContextualAction
{
    // Patterns for detecting markdown (used by IsRelevantFor)
    private static readonly Regex MarkdownDetectPattern = new(
        @"(?:^#{1,6}\s|\*\*|__|~~|```|^\s*[-*+]\s|^\s*\d+\.\s|^\s*>\s|\[.+\]\(.+\))",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public string Name => "Strip Markdown";
    public string Description => "Remove Markdown formatting to get plain text";
    public string IconName => "Text";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show when text contains Markdown-like syntax.
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        return MarkdownDetectPattern.IsMatch(selectedText);
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            var text = selectedText;

            // Remove fenced code blocks (``` ... ```) — keep the content
            text = Regex.Replace(text, @"```[\w]*\r?\n([\s\S]*?)```", "$1");

            // Remove inline code backticks — keep content
            text = Regex.Replace(text, @"`([^`]+)`", "$1");

            // Remove images: ![alt](url) → alt
            text = Regex.Replace(text, @"!\[([^\]]*)\]\([^)]+\)", "$1");

            // Remove links: [text](url) → text
            text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");

            // Remove reference links: [text][ref] → text
            text = Regex.Replace(text, @"\[([^\]]+)\]\[[^\]]*\]", "$1");

            // Remove reference definitions: [ref]: url
            text = Regex.Replace(text, @"^\s*\[[^\]]+\]:\s+.*$", "", RegexOptions.Multiline);

            // Remove headers: ## Header → Header
            text = Regex.Replace(text, @"^#{1,6}\s+", "", RegexOptions.Multiline);

            // Remove bold/italic markers (order matters — bold+italic first)
            // Use RegexOptions.Singleline so . matches newlines for multi-line formatting
            text = Regex.Replace(text, @"\*{3}(.+?)\*{3}", "$1", RegexOptions.Singleline);
            text = Regex.Replace(text, @"_{3}(.+?)_{3}", "$1", RegexOptions.Singleline);
            text = Regex.Replace(text, @"\*{2}(.+?)\*{2}", "$1", RegexOptions.Singleline);
            text = Regex.Replace(text, @"_{2}(.+?)_{2}", "$1", RegexOptions.Singleline);
            text = Regex.Replace(text, @"\*(.+?)\*", "$1", RegexOptions.Singleline);
            text = Regex.Replace(text, @"_(.+?)_", "$1", RegexOptions.Singleline);

            // Remove strikethrough
            text = Regex.Replace(text, @"~~(.+?)~~", "$1", RegexOptions.Singleline);

            // Remove blockquotes: > text → text
            text = Regex.Replace(text, @"^>\s?", "", RegexOptions.Multiline);

            // Remove horizontal rules
            text = Regex.Replace(text, @"^[-*_]{3,}\s*$", "", RegexOptions.Multiline);

            // Remove unordered list markers: - item, * item, + item → item
            text = Regex.Replace(text, @"^(\s*)[-*+]\s+", "$1", RegexOptions.Multiline);

            // Remove ordered list numbers: 1. item → item
            text = Regex.Replace(text, @"^(\s*)\d+\.\s+", "$1", RegexOptions.Multiline);

            // Strip any remaining HTML tags
            text = Regex.Replace(text, @"<[^>]+>", "");

            // Collapse excessive blank lines
            text = Regex.Replace(text, @"(\r?\n){3,}", "\n\n");
            text = text.Trim();

            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(ActionResult.FailureResult("No text remained after stripping Markdown"));

            ClipboardService.Set(text);

            return Task.FromResult(ActionResult.SuccessResult("Markdown stripped — plain text copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to strip Markdown", ex));
        }
    }
}
