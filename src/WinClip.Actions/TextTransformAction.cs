using System.Globalization;
using System.Text.RegularExpressions;

namespace WinClip.Actions;

/// <summary>
/// Built-in action that transforms selected text.
/// Free tier. Supports: UPPERCASE, lowercase, Title Case, PascalCase, camelCase, snake_case, trim.
/// </summary>
public class TextTransformAction : IAction
{
    private readonly TransformKind _kind;

    public TextTransformAction(TransformKind kind)
    {
        _kind = kind;
    }

    public string Name => _kind switch
    {
        TransformKind.Uppercase => "UPPERCASE",
        TransformKind.Lowercase => "lowercase",
        TransformKind.TitleCase => "Title Case",
        TransformKind.PascalCase => "PascalCase",
        TransformKind.CamelCase => "camelCase",
        TransformKind.SnakeCase => "snake_case",
        TransformKind.Trim => "Trim",
        _ => "Transform"
    };

    public string Description => $"Transform text to {Name}";
    public string IconName => "TextTransform";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        if (string.IsNullOrWhiteSpace(selectedText))
            return Task.FromResult(ActionResult.FailureResult("No text to transform"));

        var result = _kind switch
        {
            TransformKind.Uppercase => selectedText.ToUpperInvariant(),
            TransformKind.Lowercase => selectedText.ToLowerInvariant(),
            TransformKind.TitleCase => ToTitleCase(selectedText),
            TransformKind.PascalCase => ToPascalCase(selectedText),
            TransformKind.CamelCase => ToCamelCase(selectedText),
            TransformKind.SnakeCase => ToSnakeCase(selectedText),
            TransformKind.Trim => selectedText.Trim(),
            _ => selectedText
        };

        // Put transformed text on clipboard
        try
        {
            ClipboardService.Set(result);
            return Task.FromResult(ActionResult.SuccessWithPaste($"Transformed to {Name}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to set clipboard", ex));
        }
    }

    private static string ToTitleCase(string text)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
    }

    private static string ToPascalCase(string text)
    {
        var words = Regex.Split(text, @"[\s_\-]+")
            .Where(w => w.Length > 0)
            .ToArray();

        if (words.Length == 0) return text;

        return string.Concat(words.Select(w =>
            char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private static string ToCamelCase(string text)
    {
        var words = Regex.Split(text, @"[\s_\-]+")
            .Where(w => w.Length > 0)
            .ToArray();

        if (words.Length == 0) return text;

        return words[0].ToLowerInvariant() + string.Concat(
            words.Skip(1).Select(w =>
                char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }

    private static string ToSnakeCase(string text)
    {
        // Insert underscores before uppercase letters in camelCase/PascalCase
        var withSeparators = Regex.Replace(text, @"([a-z])([A-Z])", "$1_$2");
        // Replace any non-alphanumeric with underscore
        var normalized = Regex.Replace(withSeparators, @"[^a-zA-Z0-9]+", "_");
        return normalized.Trim('_').ToLowerInvariant();
    }
}

public enum TransformKind
{
    Uppercase,
    Lowercase,
    TitleCase,
    PascalCase,
    CamelCase,
    SnakeCase,
    Trim
}
