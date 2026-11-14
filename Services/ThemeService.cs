using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinClip.Models;

namespace WinClip.Services;

/// <summary>
/// Service that manages application theme settings and detects Windows system theme.
/// Uses Windows Registry to detect the system theme preference and supports manual theme overrides.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private readonly ILogger<ThemeService> _logger;
    private readonly IConfiguration _configuration;
    private readonly object _lockObject = new();
    private ThemeSettings _themeSettings;
    private bool _disposed;
    private readonly string _configFilePath;

    /// <summary>
    /// Registry path for Windows personalization settings.
    /// </summary>
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>
    /// Registry value name that indicates whether apps use light theme.
    /// Value of 1 = Light theme, 0 = Dark theme.
    /// </summary>
    private const string RegistryValueName = "AppsUseLightTheme";

    /// <summary>
    /// Occurs when the application theme changes.
    /// Fires when ApplyTheme is called or when system theme changes in System mode.
    /// </summary>
    public event EventHandler<ThemeMode>? ThemeChanged;

    /// <summary>
    /// Gets the currently active theme mode.
    /// Returns the effective theme (Light or Dark) based on the current settings.
    /// If System mode is selected, returns the detected system theme.
    /// </summary>
    public ThemeMode CurrentTheme
    {
        get
        {
            lock (_lockObject)
            {
                if (_themeSettings.IsSystemTheme)
                {
                    return DetectSystemTheme();
                }

                return _themeSettings.ThemeMode;
            }
        }
    }

    /// <summary>
    /// Gets the user's theme mode preference (Light, Dark, or System).
    /// This returns the configured preference, not the effective theme.
    /// When set to System, use DetectSystemTheme() to get the effective theme.
    /// </summary>
    public ThemeMode ThemeModePreference
    {
        get
        {
            lock (_lockObject)
            {
                return _themeSettings.ThemeMode;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="configuration">The application configuration.</param>
    public ThemeService(ILogger<ThemeService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        // Determine the path to appsettings.json
        _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        // Load theme settings from configuration
        _themeSettings = LoadThemeFromConfiguration();

        _logger.LogInformation("ThemeService initialized with settings from configuration: {Settings}", _themeSettings);
    }

    /// <summary>
    /// Applies the specified theme mode to the application.
    /// Updates the current theme and notifies subscribers via the ThemeChanged event.
    /// </summary>
    /// <param name="themeMode">The theme mode to apply (Light, Dark, or System).</param>
    /// <exception cref="ArgumentException">Thrown if an invalid theme mode is specified.</exception>
    public void ApplyTheme(ThemeMode themeMode)
    {
        lock (_lockObject)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ThemeService));
            }

            // Validate the theme mode
            if (!Enum.IsDefined(typeof(ThemeMode), themeMode))
            {
                throw new ArgumentException($"Invalid theme mode: {themeMode}", nameof(themeMode));
            }

            try
            {
                _logger.LogInformation("Applying theme: {ThemeMode}", themeMode);

                // Update theme settings
                _themeSettings.UpdateThemeMode(themeMode);

                // Save theme preference to configuration
                SaveThemeToConfiguration();

                // Determine the effective theme to apply
                ThemeMode effectiveTheme = themeMode == ThemeMode.System
                    ? DetectSystemTheme()
                    : themeMode;

                _logger.LogInformation("Effective theme after applying {ThemeMode}: {EffectiveTheme}", themeMode, effectiveTheme);

                // Notify subscribers of the theme change
                RaiseThemeChanged(effectiveTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply theme: {ThemeMode}", themeMode);
                throw;
            }
        }
    }

    /// <summary>
    /// Detects the current Windows system theme setting.
    /// Reads the Windows Registry to determine if the system is using light or dark theme.
    /// </summary>
    /// <returns>The detected system theme mode (Light or Dark).</returns>
    public ThemeMode DetectSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);

            if (key == null)
            {
                _logger.LogWarning("Registry key not found: {KeyPath}. Defaulting to Light theme.", RegistryKeyPath);
                return ThemeMode.Light;
            }

            var value = key.GetValue(RegistryValueName);

            if (value == null)
            {
                _logger.LogWarning("Registry value not found: {ValueName}. Defaulting to Light theme.", RegistryValueName);
                return ThemeMode.Light;
            }

            // Registry value: 1 = Light theme, 0 = Dark theme
            bool usesLightTheme = Convert.ToInt32(value) != 0;
            ThemeMode detectedTheme = usesLightTheme ? ThemeMode.Light : ThemeMode.Dark;

            _logger.LogDebug("Detected system theme from registry: {Theme} (value: {Value})", detectedTheme, value);

            return detectedTheme;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading system theme from registry. Defaulting to Light theme.");
            return ThemeMode.Light;
        }
    }

    /// <summary>
    /// Raises the ThemeChanged event to notify subscribers of a theme change.
    /// </summary>
    /// <param name="theme">The new theme that was applied.</param>
    private void RaiseThemeChanged(ThemeMode theme)
    {
        try
        {
            _logger.LogDebug("Raising ThemeChanged event for theme: {Theme}", theme);
            ThemeChanged?.Invoke(this, theme);
        }
        catch (Exception ex)
        {
            // Catch all exceptions to prevent subscriber errors from propagating
            _logger.LogError(ex, "Error occurred in ThemeChanged event handler.");
        }
    }

    /// <summary>
    /// Loads the theme settings from the application configuration.
    /// </summary>
    /// <returns>A ThemeSettings object with the configured theme mode.</returns>
    private ThemeSettings LoadThemeFromConfiguration()
    {
        try
        {
            var themeModeConfig = _configuration["Theme:ThemeMode"];

            if (!string.IsNullOrWhiteSpace(themeModeConfig) &&
                Enum.TryParse<ThemeMode>(themeModeConfig, out var configuredTheme))
            {
                _logger.LogInformation("Loaded theme from configuration: {ThemeMode}", configuredTheme);
                return new ThemeSettings(configuredTheme);
            }

            _logger.LogWarning("Theme not found in configuration or invalid. Using default: System");
            return new ThemeSettings(); // Defaults to System mode
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading theme from configuration. Using default: System");
            return new ThemeSettings();
        }
    }

    /// <summary>
    /// Saves the current theme settings to the application configuration file.
    /// </summary>
    private void SaveThemeToConfiguration()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _logger.LogWarning("Configuration file not found at: {Path}", _configFilePath);
                return;
            }

            // Read the current configuration file
            var json = File.ReadAllText(_configFilePath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Build a new JSON object with updated theme
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                // Copy all existing properties
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name == "Theme")
                    {
                        // Write updated Theme section
                        writer.WritePropertyName("Theme");
                        writer.WriteStartObject();
                        writer.WriteString("ThemeMode", _themeSettings.ThemeMode.ToString());
                        writer.WriteEndObject();
                    }
                    else
                    {
                        // Copy property as-is
                        property.WriteTo(writer);
                    }
                }

                // If Theme section didn't exist, add it
                if (!root.TryGetProperty("Theme", out _))
                {
                    writer.WritePropertyName("Theme");
                    writer.WriteStartObject();
                    writer.WriteString("ThemeMode", _themeSettings.ThemeMode.ToString());
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            // Write the updated JSON back to the file
            var updatedJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            File.WriteAllText(_configFilePath, updatedJson);

            _logger.LogInformation("Saved theme preference to configuration: {ThemeMode}", _themeSettings.ThemeMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving theme to configuration file.");
        }
    }

    /// <summary>
    /// Releases all resources used by the ThemeService.
    /// </summary>
    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogInformation("Disposing ThemeService.");

            // Clear event subscriptions
            ThemeChanged = null;

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
