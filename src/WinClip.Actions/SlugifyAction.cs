using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that converts selected text into a URL-friendly slug.
/// Example: "Hello World! This is a Test" → "hello-world-this-is-a-test"
/// </summary>
public class SlugifyAction : IAction
{
    public string Name => "Slugify";
    public string Description => "Convert text to a URL-friendly slug";
    public string IconName => "Link";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected to slugify"));

            var slug = GenerateSlug(selectedText);

            if (string.IsNullOrWhiteSpace(slug))
                return Task.FromResult(ActionResult.FailureResult("No valid characters remain after slugifying"));

            ClipboardService.Set(slug);

            return Task.FromResult(ActionResult.SuccessResult("Slug copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to slugify text", ex));
        }
    }

    private static string GenerateSlug(string text)
    {
        // Normalize Unicode characters (é → e, ñ → n, ü → u)
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var stripped = sb.ToString().Normalize(NormalizationForm.FormC);

        // Lowercase
        stripped = stripped.ToLowerInvariant();

        // Replace any non-alphanumeric character with a hyphen
        stripped = Regex.Replace(stripped, @"[^a-z0-9\s\-]", "");

        // Replace whitespace and multiple hyphens with a single hyphen
        stripped = Regex.Replace(stripped, @"[\s\-]+", "-");

        // Trim leading/trailing hyphens
        return stripped.Trim('-');
    }
}
