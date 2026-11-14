using Microsoft.Win32;

namespace WinClip.UI;

/// <summary>
/// Detects the Windows system theme (light/dark mode).
/// Reads from the registry key that Windows 10/11 uses for apps.
/// </summary>
public static class ThemeDetector
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ValueName = "AppsUseLightTheme";

    /// <summary>
    /// Returns true if Windows is set to dark mode for apps.
    /// Falls back to light mode (false) if the registry key doesn't exist.
    /// </summary>
    public static bool IsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
            var value = key?.GetValue(ValueName);
            if (value is int intValue)
                return intValue == 0; // 0 = dark, 1 = light
        }
        catch
        {
            // Registry access failed — default to light
        }

        return false;
    }

    /// <summary>
    /// Returns the recommended default theme based on the Windows system theme.
    /// </summary>
    public static PopupThemeKind GetSystemDefault() =>
        IsDarkMode() ? PopupThemeKind.Dark : PopupThemeKind.Light;
}

/// <summary>
/// Available popup theme variants. v2.1 simplified to System/Light/Dark — the legacy
/// named themes (Midnight, Charcoal, Slate, Onyx, Ocean, Forest, Nord, Monokai) are
/// preserved as enum values so existing settings.json files keep loading; they all
/// resolve to the Dark palette in <see cref="PopupTheme.GetColors"/>.
/// </summary>
public enum PopupThemeKind
{
    /// <summary>Follow the Windows system theme.</summary>
    System,
    /// <summary>Crisp light surface (white/cream) with dark text and teal accent.</summary>
    Light,
    /// <summary>Deep navy-blue dark — the default dark palette.</summary>
    Dark,
    // ── Legacy values, preserved for settings backward-compat. All map to Dark. ──
    Midnight,
    Charcoal,
    Slate,
    Onyx,
    Ocean,
    Forest,
    Nord,
    Monokai
}

/// <summary>
/// Immutable color palette for a single popup theme.
/// No shadows — uses clean border strokes for definition.
/// </summary>
public sealed class PopupThemeColors
{
    public string Background { get; init; } = "#1E293B";
    public string BackgroundGradientEnd { get; init; } = "#1A2332";
    public string Border { get; init; } = "#334155";
    public string Text { get; init; } = "#94A3B8";
    public string TextHover { get; init; } = "#E2E8F0";
    public string Separator { get; init; } = "#2D3A4D";
    public string ButtonBackground { get; init; } = "#273449";
    public string ButtonHover { get; init; } = "#334155";
    public string AccentColor { get; init; } = "#5EEAD4";
    public string SuccessColor { get; init; } = "#10B981";
    public string FailureColor { get; init; } = "#EF4444";
    public string IconColor { get; init; } = "#5EEAD4";
    public string SelectionBackground { get; init; } = "#143D3B";
    public double CornerRadius { get; init; } = 12;
}

/// <summary>
/// Central registry of all popup theme palettes. Immutable after construction.
/// </summary>
public static class PopupTheme
{
    /// <summary>
    /// Resolves a <see cref="PopupThemeKind"/> to its concrete color palette.
    /// <c>System</c> is resolved via <see cref="ThemeDetector"/>.
    /// </summary>
    public static PopupThemeColors GetColors(PopupThemeKind kind)
    {
        if (kind == PopupThemeKind.System)
            kind = ThemeDetector.GetSystemDefault();

        return kind switch
        {
            PopupThemeKind.Light => Light,
            PopupThemeKind.Dark => Midnight,
            // Legacy named themes — all map to the Dark palette.
            PopupThemeKind.Midnight => Midnight,
            PopupThemeKind.Charcoal => Midnight,
            PopupThemeKind.Slate => Midnight,
            PopupThemeKind.Onyx => Midnight,
            PopupThemeKind.Ocean => Midnight,
            PopupThemeKind.Forest => Midnight,
            PopupThemeKind.Nord => Midnight,
            PopupThemeKind.Monokai => Midnight,
            _ => Midnight,
        };
    }

    // ── Theme definitions ──────────────────────────────────────────────

    /// <summary>Crisp light surface — white background, dark text, teal accent.</summary>
    public static readonly PopupThemeColors Light = new()
    {
        Background = "#FFFFFF",
        BackgroundGradientEnd = "#FFFFFF",
        Border = "#E2E8F0",
        Text = "#475569",
        TextHover = "#0F172A",
        Separator = "#E2E8F0",
        ButtonBackground = "#F1F5F9",
        ButtonHover = "#E2E8F0",
        SelectionBackground = "#CCFBF1",
        AccentColor = "#0F766E",
        SuccessColor = "#10B981",
        FailureColor = "#DC2626",
        IconColor = "#0D9488",
        CornerRadius = 12,
    };

    /// <summary>Deep navy-blue dark.</summary>
    public static readonly PopupThemeColors Midnight = new()
    {
        Background = "#0F172A",
        BackgroundGradientEnd = "#0F172A",
        Border = "#1E293B",
        Text = "#94A3B8",
        TextHover = "#E2E8F0",
        Separator = "#1E293B",
        ButtonBackground = "#1E293B",
        ButtonHover = "#283548",
        AccentColor = "#5EEAD4",
        SuccessColor = "#5EEAD4",
        FailureColor = "#F87171",
        IconColor = "#5EEAD4",
        CornerRadius = 12,
    };

    /// <summary>Warm dark grey with brown undertones.</summary>
    public static readonly PopupThemeColors Charcoal = new()
    {
        Background = "#1C1917",
        BackgroundGradientEnd = "#1A1815",
        Border = "#44403C",
        Text = "#A8A29E",
        TextHover = "#E7E5E4",
        Separator = "#33302C",
        ButtonBackground = "#292524",
        ButtonHover = "#3B3735",
        AccentColor = "#FB923C",
        SuccessColor = "#FB923C",
        FailureColor = "#F87171",
        IconColor = "#FB923C",
        CornerRadius = 12,
    };

    /// <summary>Cool blue-grey mid-dark.</summary>
    public static readonly PopupThemeColors Slate = new()
    {
        Background = "#1E293B",
        BackgroundGradientEnd = "#1A2332",
        Border = "#334155",
        Text = "#94A3B8",
        TextHover = "#E2E8F0",
        Separator = "#2D3A4D",
        ButtonBackground = "#273449",
        ButtonHover = "#334155",
        AccentColor = "#38BDF8",
        SuccessColor = "#38BDF8",
        FailureColor = "#FB923C",
        IconColor = "#38BDF8",
        CornerRadius = 12,
    };

    /// <summary>Pure deep black with minimal color.</summary>
    public static readonly PopupThemeColors Onyx = new()
    {
        Background = "#0A0A0A",
        BackgroundGradientEnd = "#0D0D0F",
        Border = "#262626",
        Text = "#A3A3A3",
        TextHover = "#E5E5E5",
        Separator = "#1A1A1A",
        ButtonBackground = "#171717",
        ButtonHover = "#262626",
        AccentColor = "#A78BFA",
        SuccessColor = "#A78BFA",
        FailureColor = "#F87171",
        IconColor = "#A78BFA",
        CornerRadius = 12,
    };

    /// <summary>Deep teal/navy with cyan accents.</summary>
    public static readonly PopupThemeColors Ocean = new()
    {
        Background = "#0C1222",
        BackgroundGradientEnd = "#0F1729",
        Border = "#1B3A4B",
        Text = "#8FAABB",
        TextHover = "#D5E6F0",
        Separator = "#163040",
        ButtonBackground = "#132638",
        ButtonHover = "#1A3548",
        AccentColor = "#22D3EE",
        SuccessColor = "#22D3EE",
        FailureColor = "#FB923C",
        IconColor = "#22D3EE",
        CornerRadius = 12,
    };

    /// <summary>Dark green-grey with emerald accents.</summary>
    public static readonly PopupThemeColors Forest = new()
    {
        Background = "#0C1A12",
        BackgroundGradientEnd = "#0E1C15",
        Border = "#1B3828",
        Text = "#8FAA96",
        TextHover = "#D4E7DA",
        Separator = "#163022",
        ButtonBackground = "#152B1E",
        ButtonHover = "#1E3828",
        AccentColor = "#34D399",
        SuccessColor = "#34D399",
        FailureColor = "#FB923C",
        IconColor = "#34D399",
        CornerRadius = 12,
    };

    /// <summary>Arctic blue-grey palette (Nord-inspired).</summary>
    public static readonly PopupThemeColors Nord = new()
    {
        Background = "#2E3440",
        BackgroundGradientEnd = "#292E38",
        Border = "#434C5E",
        Text = "#D8DEE9",
        TextHover = "#ECEFF4",
        Separator = "#3B4252",
        ButtonBackground = "#3B4252",
        ButtonHover = "#434C5E",
        AccentColor = "#88C0D0",
        SuccessColor = "#88C0D0",
        FailureColor = "#BF616A",
        IconColor = "#88C0D0",
        CornerRadius = 12,
    };

    /// <summary>Warm dark with signature pink/green accents (Monokai-inspired).</summary>
    public static readonly PopupThemeColors Monokai = new()
    {
        Background = "#272822",
        BackgroundGradientEnd = "#23241E",
        Border = "#49483E",
        Text = "#A6A28C",
        TextHover = "#F8F8F2",
        Separator = "#3E3D32",
        ButtonBackground = "#3E3D32",
        ButtonHover = "#49483E",
        AccentColor = "#F92672",
        SuccessColor = "#A6E22E",
        FailureColor = "#F92672",
        IconColor = "#F92672",
        CornerRadius = 12,
    };
}
