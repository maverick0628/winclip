using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WinClip.Models;

namespace WinClip.Services;

/// <summary>
/// Service for persisting and loading application settings to/from JSON file.
/// Settings are stored in %AppData%/WinClip/settings.json.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the SettingsService class.
    /// </summary>
    public SettingsService()
    {
        // Get AppData folder and create WinClip subfolder path
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var winClipPath = Path.Combine(appDataPath, "WinClip");
        _settingsFilePath = Path.Combine(winClipPath, "settings.json");

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Initializes a new instance of the SettingsService class with a custom settings path.
    /// Used primarily for testing.
    /// </summary>
    /// <param name="settingsFilePath">The custom path to the settings file.</param>
    public SettingsService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Loads settings from the default settings file.
    /// If the file doesn't exist or cannot be loaded, returns default settings.
    /// </summary>
    /// <returns>The loaded AppSettings instance or default settings if loading fails.</returns>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            // If file doesn't exist, return defaults
            if (!File.Exists(_settingsFilePath))
            {
                return AppSettings.CreateDefault();
            }

            // Read and deserialize the settings file
            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            // Return settings if successfully deserialized, otherwise return defaults
            return settings ?? AppSettings.CreateDefault();
        }
        catch (JsonException)
        {
            // JSON parsing error - return defaults
            return AppSettings.CreateDefault();
        }
        catch (IOException)
        {
            // File access error - return defaults
            return AppSettings.CreateDefault();
        }
        catch (UnauthorizedAccessException)
        {
            // Permission error - return defaults
            return AppSettings.CreateDefault();
        }
    }

    /// <summary>
    /// Saves the provided settings to the default settings file.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when settings is null.</exception>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Serialize and write settings to file
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Failed to save settings to {_settingsFilePath}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException($"Access denied when saving settings to {_settingsFilePath}", ex);
        }
    }

    /// <summary>
    /// Gets the path to the settings file.
    /// </summary>
    /// <returns>The full path to the settings.json file.</returns>
    public string GetSettingsPath()
    {
        return _settingsFilePath;
    }

    /// <summary>
    /// Resets settings to default values and saves them.
    /// </summary>
    /// <returns>A task representing the asynchronous reset operation.</returns>
    public async Task ResetToDefaultsAsync()
    {
        var defaultSettings = AppSettings.CreateDefault();
        await SaveSettingsAsync(defaultSettings);
    }

    /// <summary>
    /// Exports the current settings to a user-specified file path.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    /// <param name="filePath">The full path where settings should be exported.</param>
    /// <returns>A task representing the asynchronous export operation.</returns>
    /// <exception cref="ArgumentException">Thrown when filePath is null or empty.</exception>
    public async Task ExportSettingsAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        try
        {
            // Load current settings
            var settings = await LoadSettingsAsync();

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Serialize and write settings to file
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException($"Failed to export settings to {filePath}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException($"Access denied when exporting settings to {filePath}", ex);
        }
    }

    /// <summary>
    /// Imports settings from a user-specified file path.
    /// If the file doesn't exist or cannot be loaded, returns default settings.
    /// </summary>
    /// <param name="filePath">The full path to the settings file to import.</param>
    /// <returns>The loaded AppSettings instance or default settings if loading fails.</returns>
    /// <exception cref="ArgumentException">Thrown when filePath is null or empty.</exception>
    public async Task<AppSettings> ImportSettingsAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        try
        {
            // If file doesn't exist, return defaults
            if (!File.Exists(filePath))
            {
                return AppSettings.CreateDefault();
            }

            // Read and deserialize the settings file
            var json = await File.ReadAllTextAsync(filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            // Return settings if successfully deserialized, otherwise return defaults
            return settings ?? AppSettings.CreateDefault();
        }
        catch (JsonException)
        {
            // JSON parsing error - return defaults
            return AppSettings.CreateDefault();
        }
        catch (IOException)
        {
            // File access error - return defaults
            return AppSettings.CreateDefault();
        }
        catch (UnauthorizedAccessException)
        {
            // Permission error - return defaults
            return AppSettings.CreateDefault();
        }
    }
}
