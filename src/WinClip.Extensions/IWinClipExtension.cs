namespace WinClip.Extensions;

/// <summary>
/// Defines the contract for all WinClip extensions.
/// Extensions are custom actions that can be loaded from external DLLs.
/// </summary>
public interface IWinClipExtension
{
    /// <summary>
    /// Gets the unique identifier for this extension.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the extension.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a brief description of what the extension does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of the extension.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the author of the extension.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets the name of the icon to display for this extension.
    /// </summary>
    string IconName { get; }

    /// <summary>
    /// Initializes the extension with configuration settings.
    /// </summary>
    /// <param name="settings">A dictionary of configuration settings for the extension.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    Task InitializeAsync(Dictionary<string, object> settings);

    /// <summary>
    /// Executes the extension asynchronously on the selected text.
    /// </summary>
    /// <param name="selectedText">The text that was selected by the user.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the extension execution.</returns>
    Task<ExtensionResult> ExecuteAsync(string selectedText);

    /// <summary>
    /// Gets the configuration schema for the extension's settings.
    /// </summary>
    /// <returns>A dictionary describing the configuration options available for this extension.</returns>
    Dictionary<string, SettingDefinition> GetSettingsSchema();
}

/// <summary>
/// Represents the result of an extension execution.
/// </summary>
public class ExtensionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the result text to be used.
    /// </summary>
    public string? ResultText { get; set; }

    /// <summary>
    /// Gets or sets an error message if the execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the execution result.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Defines a configuration setting for an extension.
/// </summary>
public class SettingDefinition
{
    /// <summary>
    /// Gets or sets the display name of the setting.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the setting.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the setting (e.g., "string", "int", "bool").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default value for the setting.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the setting is required.
    /// </summary>
    public bool Required { get; set; }
}
