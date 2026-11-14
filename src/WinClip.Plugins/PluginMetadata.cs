namespace WinClip.Plugins;

/// <summary>
/// Contains descriptive information about a plugin, including its identity,
/// version, author, and required permissions.
/// </summary>
public class PluginMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier for the plugin.
    /// This should be a reverse-domain notation (e.g., "com.example.myplugin").
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the plugin.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the version of the plugin using semantic versioning (e.g., "1.0.0").
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the author or organization that created the plugin.
    /// </summary>
    public required string Author { get; set; }

    /// <summary>
    /// Gets or sets a brief description of what the plugin does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the URL to the plugin's website or repository.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Gets or sets the path to the plugin's icon file (relative to plugin directory).
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Gets or sets the minimum WinClip API version required by this plugin.
    /// </summary>
    public string? MinApiVersion { get; set; }

    /// <summary>
    /// Gets or sets the permissions required by this plugin.
    /// These permissions will be requested from the user during installation or first use.
    /// </summary>
    public ICollection<string> RequiredPermissions { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets any tags or categories associated with the plugin for discovery.
    /// </summary>
    public ICollection<string> Tags { get; set; } = new List<string>();
}
