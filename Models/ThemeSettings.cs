namespace WinClip.Models;

/// <summary>
/// Defines the available theme modes for the application.
/// </summary>
public enum ThemeMode
{
    /// <summary>
    /// Use light theme colors.
    /// </summary>
    Light,

    /// <summary>
    /// Use dark theme colors.
    /// </summary>
    Dark,

    /// <summary>
    /// Automatically follow the Windows system theme setting.
    /// </summary>
    System
}

/// <summary>
/// Represents the theme configuration settings for the application.
/// Contains information about the current theme mode and related preferences.
/// </summary>
public sealed class ThemeSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeSettings"/> class with the specified theme mode.
    /// </summary>
    /// <param name="themeMode">The theme mode to apply.</param>
    public ThemeSettings(ThemeMode themeMode)
    {
        ThemeMode = themeMode;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeSettings"/> class with default settings.
    /// Defaults to System theme mode.
    /// </summary>
    public ThemeSettings()
        : this(ThemeMode.System)
    {
    }

    /// <summary>
    /// Gets or sets the current theme mode for the application.
    /// </summary>
    public ThemeMode ThemeMode { get; set; }

    /// <summary>
    /// Gets the UTC timestamp when the theme settings were last updated.
    /// </summary>
    public DateTime LastUpdated { get; private set; }

    /// <summary>
    /// Updates the theme mode and refreshes the last updated timestamp.
    /// </summary>
    /// <param name="newThemeMode">The new theme mode to apply.</param>
    public void UpdateThemeMode(ThemeMode newThemeMode)
    {
        ThemeMode = newThemeMode;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a value indicating whether the theme mode is set to follow the system theme.
    /// </summary>
    public bool IsSystemTheme => ThemeMode == ThemeMode.System;

    /// <summary>
    /// Gets a value indicating whether the theme mode is explicitly set to light or dark.
    /// </summary>
    public bool IsManualOverride => ThemeMode != ThemeMode.System;

    /// <summary>
    /// Returns a string representation of the theme settings.
    /// </summary>
    /// <returns>A string containing the theme mode and last updated timestamp.</returns>
    public override string ToString()
    {
        return $"ThemeSettings: {ThemeMode} - Updated {LastUpdated:yyyy-MM-dd HH:mm:ss.fff} UTC";
    }
}
