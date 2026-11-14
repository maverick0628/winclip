using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that provides detailed text statistics beyond basic word count.
/// Includes: characters, words, sentences, paragraphs, lines, unique words,
/// and estimated reading time.
/// </summary>
public class TextStatsAction : IAction
{
    // Average reading speed: 238 words per minute (Brysbaert, 2019)
    private const double WordsPerMinute = 238.0;

    private static readonly Regex WordPattern = new(
        @"\b[\w']+\b",
        RegexOptions.Compiled);

    private static readonly Regex SentencePattern = new(
        @"[.!?]+(?:\s|$)",
        RegexOptions.Compiled);

    public string Name => "Text Statistics";
    public string Description => "Detailed text analysis: words, sentences, reading time, and more";
    public string IconName => "Statistics";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected to analyze"));

            var chars = selectedText.Length;
            var charsNoSpaces = selectedText.Count(c => !char.IsWhiteSpace(c));

            var words = WordPattern.Matches(selectedText);
            var wordCount = words.Count;

            var uniqueWords = words
                .Select(m => m.Value.ToLowerInvariant())
                .Distinct()
                .Count();

            var sentences = SentencePattern.Matches(selectedText).Count;
            if (sentences == 0 && wordCount > 0) sentences = 1; // Text without punctuation = 1 sentence

            var lines = selectedText.Split(["\r\n", "\n"], StringSplitOptions.None).Length;

            var paragraphs = Regex.Split(selectedText, @"\n\s*\n")
                .Count(p => !string.IsNullOrWhiteSpace(p));
            if (paragraphs == 0 && wordCount > 0) paragraphs = 1;

            // Reading time
            var readingMinutes = wordCount / WordsPerMinute;
            var readingTime = readingMinutes < 1
                ? $"{Math.Max(1, (int)Math.Ceiling(readingMinutes * 60))} sec"
                : $"{readingMinutes:F1} min";

            // Average word length
            var avgWordLen = wordCount > 0
                ? words.Average(m => m.Value.Length)
                : 0;

            var stats = string.Join(Environment.NewLine,
                $"Characters:    {chars:N0}",
                $"Chars (no ws): {charsNoSpaces:N0}",
                $"Words:         {wordCount:N0}",
                $"Unique words:  {uniqueWords:N0}",
                $"Avg word len:  {avgWordLen:F1}",
                $"Sentences:     {sentences:N0}",
                $"Paragraphs:    {paragraphs:N0}",
                $"Lines:         {lines:N0}",
                $"Reading time:  ~{readingTime}");

            ClipboardService.Set(stats);

            return Task.FromResult(ActionResult.SuccessResult(
                $"{wordCount:N0} words, {sentences} sentences, ~{readingTime} read"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to compute text statistics", ex));
        }
    }
}
