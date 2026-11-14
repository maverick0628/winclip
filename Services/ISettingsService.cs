using WinClip.Models;

namespace WinClip.Services;

/// <summary>
/// Service interface for persisting and loading application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from the default settings file.
    /// If the file doesn't exist or cannot be loaded, returns default settings.
    /// </summary>
    /// <returns>The loaded AppSettings instance or default settings if loading fails.</returns>
    Task<AppSettings> LoadSettingsAsync();

    /// <summary>
    /// Saves the provided settings to the default settings file.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Gets the path to the settings file.
    /// </summary>
    /// <returns>The full path to the settings.json file.</returns>
    string GetSettingsPath();

    /// <summary>
    /// Resets settings to default values and saves them.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    Task ResetToDefaultsAsync();

    /// <summary>
    /// Exports the current settings to a user-specified file path.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    /// <param name="filePath">The full path where settings should be exported.</param>
    /// <returns>A task representing the asynchronous export operation.</returns>
    Task ExportSettingsAsync(string filePath);

    /// <summary>
    /// Imports settings from a user-specified file path.
    /// If the file doesn't exist or cannot be loaded, returns default settings.
    /// </summary>
    /// <param name="filePath">The full path to the settings file to import.</param>
    /// <returns>The loaded AppSettings instance or default settings if loading fails.</returns>
    Task<AppSettings> ImportSettingsAsync(string filePath);
}
