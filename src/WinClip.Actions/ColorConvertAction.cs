using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that converts colors between hex (#RRGGBB), rgb(r, g, b), and hsl(h, s%, l%) formats.
/// Contextual: only appears when text looks like a color value.
/// Outputs all three representations.
/// </summary>
public class ColorConvertAction : IAction, IContextualAction
{
    private static readonly Regex HexPattern = new(
        @"^\s*#?([0-9a-fA-F]{3}|[0-9a-fA-F]{6})\s*$",
        RegexOptions.Compiled);

    private static readonly Regex RgbPattern = new(
        @"^\s*rgba?\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*(?:,\s*[\d.]+\s*)?\)\s*$",
        RegexOptions.Compiled);

    private static readonly Regex HslPattern = new(
        @"^\s*hsla?\(\s*(\d{1,3})\s*,\s*(\d{1,3})%?\s*,\s*(\d{1,3})%?\s*(?:,\s*[\d.]+\s*)?\)\s*$",
        RegexOptions.Compiled);

    public string Name => "Color Convert";
    public string Description => "Convert between hex, rgb(), and hsl() color formats";
    public string IconName => "Palette";

    public bool IsAvailable() => true;

    public bool IsRelevantFor(string selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText)) return false;
        var t = selectedText.Trim();
        return HexPattern.IsMatch(t) || RgbPattern.IsMatch(t) || HslPattern.IsMatch(t);
    }

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected"));

            var trimmed = selectedText.Trim();

            int r, g, b;

            var hexMatch = HexPattern.Match(trimmed);
            if (hexMatch.Success)
            {
                var hex = hexMatch.Groups[1].Value;
                if (hex.Length == 3)
                    hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";

                r = int.Parse(hex[..2], NumberStyles.HexNumber);
                g = int.Parse(hex[2..4], NumberStyles.HexNumber);
                b = int.Parse(hex[4..6], NumberStyles.HexNumber);
            }
            else if (RgbPattern.Match(trimmed) is { Success: true } rgbMatch)
            {
                r = int.Parse(rgbMatch.Groups[1].Value);
                g = int.Parse(rgbMatch.Groups[2].Value);
                b = int.Parse(rgbMatch.Groups[3].Value);

                if (r > 255 || g > 255 || b > 255)
                    return Task.FromResult(ActionResult.FailureResult("RGB values must be 0-255"));
            }
            else if (HslPattern.Match(trimmed) is { Success: true } hslMatch)
            {
                var h = int.Parse(hslMatch.Groups[1].Value);
                var s = int.Parse(hslMatch.Groups[2].Value);
                var l = int.Parse(hslMatch.Groups[3].Value);

                if (h > 360 || s > 100 || l > 100)
                    return Task.FromResult(ActionResult.FailureResult("HSL values out of range (H:0-360, S:0-100, L:0-100)"));

                (r, g, b) = HslToRgb(h, s, l);
            }
            else
            {
                return Task.FromResult(ActionResult.FailureResult("Not a recognized color format"));
            }

            // Convert RGB to all formats
            var hexStr = $"#{r:X2}{g:X2}{b:X2}".ToLowerInvariant();
            var rgbStr = $"rgb({r}, {g}, {b})";
            var (hue, sat, lit) = RgbToHsl(r, g, b);
            var hslStr = $"hsl({hue}, {sat}%, {lit}%)";

            var result = $"{hexStr}\n{rgbStr}\n{hslStr}";
            ClipboardService.Set(result);

            return Task.FromResult(ActionResult.SuccessResult($"{hexStr} / {rgbStr} / {hslStr}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult("Failed to convert color", ex));
        }
    }

    private static (int H, int S, int L) RgbToHsl(int r, int g, int b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double l = (max + min) / 2.0;
        double h = 0, s = 0;

        if (max != min)
        {
            double d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

            if (max == rd)
                h = ((gd - bd) / d) + (gd < bd ? 6 : 0);
            else if (max == gd)
                h = ((bd - rd) / d) + 2;
            else
                h = ((rd - gd) / d) + 4;

            h /= 6.0;
        }

        return ((int)Math.Round(h * 360), (int)Math.Round(s * 100), (int)Math.Round(l * 100));
    }

    private static (int R, int G, int B) HslToRgb(int h, int s, int l)
    {
        double hd = h / 360.0, sd = s / 100.0, ld = l / 100.0;

        if (sd == 0)
        {
            var v = (int)Math.Round(ld * 255);
            return (v, v, v);
        }

        double q = ld < 0.5 ? ld * (1 + sd) : ld + sd - ld * sd;
        double p = 2 * ld - q;

        return (
            (int)Math.Round(HueToRgb(p, q, hd + 1.0 / 3.0) * 255),
            (int)Math.Round(HueToRgb(p, q, hd) * 255),
            (int)Math.Round(HueToRgb(p, q, hd - 1.0 / 3.0) * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        return p;
    }
}
