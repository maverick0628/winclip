using WinClip.Models;

namespace WinClip.Services;

/// <summary>
/// Service interface for managing application theme settings and detecting Windows system theme.
/// Provides theme switching capabilities and automatic system theme detection.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Occurs when the application theme changes.
    /// Fires when ApplyTheme is called or when system theme changes in System mode.
    /// </summary>
    event EventHandler<ThemeMode>? ThemeChanged;

    /// <summary>
    /// Gets the currently active theme mode.
    /// Returns the effective theme (Light or Dark) based on the current settings.
    /// </summary>
    ThemeMode CurrentTheme { get; }

    /// <summary>
    /// Gets the user's theme mode preference (Light, Dark, or System).
    /// This returns the configured preference, not the effective theme.
    /// When set to System, use DetectSystemTheme() to get the effective theme.
    /// </summary>
    ThemeMode ThemeModePreference { get; }

    /// <summary>
    /// Applies the specified theme mode to the application.
    /// Updates the current theme and notifies subscribers via the ThemeChanged event.
    /// </summary>
    /// <param name="themeMode">The theme mode to apply (Light, Dark, or System).</param>
    /// <exception cref="ArgumentException">Thrown if an invalid theme mode is specified.</exception>
    void ApplyTheme(ThemeMode themeMode);

    /// <summary>
    /// Detects the current Windows system theme setting.
    /// Reads the Windows Registry to determine if the system is using light or dark theme.
    /// </summary>
    /// <returns>The detected system theme mode (Light or Dark).</returns>
    ThemeMode DetectSystemTheme();
}
