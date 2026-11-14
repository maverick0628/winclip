using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace WinClip.Actions.Image;

/// <summary>
/// Image-clipboard utilities — Save Image to file, decode QR codes from clipboard images.
/// Tray-triggered (no popup integration); each method assumes the caller verifies
/// <see cref="HasClipboardImage"/> first.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ClipboardImageService
{
    /// <summary>True if the system clipboard currently holds an image.</summary>
    public static bool HasClipboardImage()
    {
        try { return Clipboard.ContainsImage(); }
        catch { return false; }
    }

    /// <summary>
    /// Saves the clipboard image to <paramref name="targetPath"/> as PNG.
    /// Returns the saved size in bytes, or null on failure.
    /// </summary>
    public static long? SaveAsPng(string targetPath)
    {
        if (!HasClipboardImage()) return null;

        try
        {
            var source = Clipboard.GetImage();
            if (source == null) return null;

            using var stream = File.Create(targetPath);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(stream);
            stream.Flush();
            return stream.Length;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to decode a QR code (or other supported barcode) from the clipboard image.
    /// Returns the decoded text, or null if no barcode was found.
    /// </summary>
    public static string? DecodeBarcode()
    {
        if (!HasClipboardImage()) return null;

        try
        {
            var source = Clipboard.GetImage();
            if (source == null) return null;

            using var bitmap = ToBitmap(source);
            var reader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[]
                    {
                        BarcodeFormat.QR_CODE,
                        BarcodeFormat.DATA_MATRIX,
                        BarcodeFormat.AZTEC,
                        BarcodeFormat.PDF_417,
                        BarcodeFormat.EAN_13,
                        BarcodeFormat.CODE_128,
                    }
                }
            };

            var result = reader.Decode(bitmap);
            return result?.Text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Convert a WPF <see cref="BitmapSource"/> to a System.Drawing.Bitmap.</summary>
    private static Bitmap ToBitmap(BitmapSource source)
    {
        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        encoder.Save(stream);
        stream.Position = 0;
        return new Bitmap(stream);
    }

    /// <summary>Suggested filename for a saved clipboard image: timestamped PNG.</summary>
    public static string SuggestedFilename() =>
        $"clipboard-{DateTime.Now:yyyyMMdd-HHmmss}.png";
}
