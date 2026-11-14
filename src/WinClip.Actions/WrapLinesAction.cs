using System.Text;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that word-wraps text at a fixed column width or unwraps to a single line.
/// Two modes via <see cref="WrapMode"/>: Wrap (at 80 chars) and Unwrap (join lines).
/// </summary>
public class WrapLinesAction : IAction
{
    private readonly WrapMode _mode;
    private const int WrapWidth = 80;

    public WrapLinesAction(WrapMode mode)
    {
        _mode = mode;
    }

    public string Name => _mode switch
    {
        WrapMode.Wrap => "Wrap Lines (80 cols)",
        WrapMode.Unwrap => "Unwrap to Single Line",
        _ => "Wrap Lines"
    };

    public string Description => _mode switch
    {
        WrapMode.Wrap => $"Word-wrap text at {WrapWidth} characters per line",
        WrapMode.Unwrap => "Join all lines into a single continuous line",
        _ => "Wrap or unwrap text"
    };

    public string IconName => "Wrap";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            string result;

            if (_mode == WrapMode.Unwrap)
            {
                // Join all lines with a single space, collapse multiple spaces
                result = string.Join(" ", selectedText
                    .Split(["\r\n", "\n"], StringSplitOptions.None)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0));
            }
            else
            {
                result = WordWrap(selectedText, WrapWidth);
            }

            ClipboardService.Set(result);

            var label = _mode == WrapMode.Wrap ? $"Wrapped at {WrapWidth} columns" : "Unwrapped to single line";
            return Task.FromResult(ActionResult.SuccessResult($"{label} — copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to wrap/unwrap text", ex));
        }
    }

    private static string WordWrap(string text, int maxWidth)
    {
        // Process each paragraph (double-newline separated) independently
        var paragraphs = text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.None);
        var wrappedParagraphs = new List<string>();

        foreach (var para in paragraphs)
        {
            // Join lines within the paragraph, then re-wrap
            var words = para.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                wrappedParagraphs.Add("");
                continue;
            }

            var sb = new StringBuilder();
            var lineLength = 0;

            foreach (var word in words)
            {
                if (lineLength == 0)
                {
                    sb.Append(word);
                    lineLength = word.Length;
                }
                else if (lineLength + 1 + word.Length <= maxWidth)
                {
                    sb.Append(' ').Append(word);
                    lineLength += 1 + word.Length;
                }
                else
                {
                    sb.AppendLine();
                    sb.Append(word);
                    lineLength = word.Length;
                }
            }

            wrappedParagraphs.Add(sb.ToString());
        }

        return string.Join(Environment.NewLine + Environment.NewLine, wrappedParagraphs);
    }
}

public enum WrapMode
{
    Wrap,
    Unwrap
}
