using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that converts numbers between Decimal, Hexadecimal, and Binary.
/// Contextual: only appears when selected text looks like a number in one of
/// these bases (detects 0x prefix for hex, 0b prefix for binary, or plain digits).
/// Outputs all three representations.
/// </summary>
public class NumberBaseConvertAction : IAction, IContextualAction
{
    private static readonly Regex HexPattern = new(
        @"^\s*0[xX][0-9a-fA-F]+\s*$",
        RegexOptions.Compiled);

    private static readonly Regex BinaryPattern = new(
        @"^\s*0[bB][01]+\s*$",
        RegexOptions.Compiled);

    private static readonly Regex DecimalPattern = new(
        @"^\s*-?\d+\s*$",
        RegexOptions.Compiled);

    public string Name => "Number Base Convert";
    public string Description => "Convert between decimal, hex (0x), and binary (0b)";
    public string IconName => "Calculator";

    public bool IsAvailable() => true;

    /// <summary>
    /// Only show when text looks like a number in hex, binary, or decimal.
    /// </summary>
    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        var trimmed = selectedText.Trim();

        return HexPattern.IsMatch(trimmed) ||
               BinaryPattern.IsMatch(trimmed) ||
               DecimalPattern.IsMatch(trimmed);
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            var trimmed = selectedText.Trim();
            long value;
            string sourceBase;

            if (HexPattern.IsMatch(trimmed))
            {
                // Remove 0x prefix and parse
                var hex = trimmed[2..];
                value = Convert.ToInt64(hex, 16);
                sourceBase = "hex";
            }
            else if (BinaryPattern.IsMatch(trimmed))
            {
                // Remove 0b prefix and parse
                var bin = trimmed[2..];
                value = Convert.ToInt64(bin, 2);
                sourceBase = "binary";
            }
            else if (DecimalPattern.IsMatch(trimmed))
            {
                if (!long.TryParse(trimmed, out value))
                    return Task.FromResult(ActionResult.FailureResult("Number too large to convert"));
                sourceBase = "decimal";
            }
            else
            {
                return Task.FromResult(ActionResult.FailureResult("Not a recognized number format"));
            }

            // Build all three representations
            var dec = value.ToString();
            var hex2 = $"0x{value:X}";
            var bin2 = $"0b{Convert.ToString(value, 2)}";

            var result = $"Dec: {dec}\nHex: {hex2}\nBin: {bin2}";

            ClipboardService.Set(result);

            return Task.FromResult(ActionResult.SuccessResult($"Converted from {sourceBase} — all bases copied"));
        }
        catch (OverflowException)
        {
            return Task.FromResult(ActionResult.FailureResult("Number is too large to convert"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to convert number base", ex));
        }
    }
}
