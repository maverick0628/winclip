using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace WinClip.UI;

/// <summary>
/// Loads the WinClip tray icon from the embedded, pre-rendered multi-resolution
/// .ico bundles built from the canonical brand assets in <c>brand/tray/</c>.
///
/// Two states are exposed via <see cref="CreateTrayIcon"/>:
///   * Enabled — full-contrast white <c>[W]</c> mark (resource <c>WinClip.TrayIcon.Enabled</c>)
///   * Disabled / paused — slate <c>[W]</c> mark, reads as dimmed on dark trays
///     (resource <c>WinClip.TrayIcon.Disabled</c>).
///
/// The .ico bundles each carry 16/20/24/32/48 px frames so Windows picks the
/// matching size for the current tray DPI without runtime scaling.
/// </summary>
internal static class IconGenerator
{
    private const string EnabledResource = "WinClip.TrayIcon.Enabled";
    private const string DisabledResource = "WinClip.TrayIcon.Disabled";

    /// <summary>
    /// Returns the tray icon for the requested state. The caller owns the Icon
    /// and is responsible for disposing it.
    /// </summary>
    public static Icon CreateTrayIcon(bool paused = false)
    {
        var resourceName = paused ? DisabledResource : EnabledResource;
        var asm = typeof(IconGenerator).Assembly;
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded tray icon resource not found: '{resourceName}'. " +
                $"Available resources: {string.Join(", ", asm.GetManifestResourceNames())}");
        return new Icon(stream);
    }

    /// <summary>
    /// Returns a WPF <see cref="System.Windows.Media.ImageSource"/> built from
    /// the enabled tray icon for use as a window/title-bar icon.
    /// </summary>
    public static System.Windows.Media.ImageSource CreateWindowIcon()
    {
        using var icon = CreateTrayIcon(paused: false);
        return Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            System.Windows.Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
    }
}
