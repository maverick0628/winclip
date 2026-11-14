using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinClip.Plugins;

/// <summary>
/// Represents a plugin manifest file (manifest.json) that describes a plugin's
/// metadata, requirements, and permissions. This class handles serialization and
/// deserialization of manifest files.
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// Gets or sets the unique identifier for the plugin.
    /// This should be a reverse-domain notation (e.g., "com.example.myplugin").
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name of the plugin.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the version of the plugin using semantic versioning (e.g., "1.0.0").
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the author or organization that created the plugin.
    /// </summary>
    [JsonPropertyName("author")]
    public required string Author { get; set; }

    /// <summary>
    /// Gets or sets a brief description of what the plugin does.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the URL to the plugin's website or repository.
    /// </summary>
    [JsonPropertyName("website")]
    public string? Website { get; set; }

    /// <summary>
    /// Gets or sets the path to the plugin's icon file (relative to plugin directory).
    /// </summary>
    [JsonPropertyName("iconPath")]
    public string? IconPath { get; set; }

    /// <summary>
    /// Gets or sets the minimum WinClip API version required by this plugin.
    /// </summary>
    [JsonPropertyName("minApiVersion")]
    public string? MinApiVersion { get; set; }

    /// <summary>
    /// Gets or sets the permissions required by this plugin.
    /// These permissions will be requested from the user during installation or first use.
    /// </summary>
    [JsonPropertyName("permissions")]
    public ICollection<string> Permissions { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets any tags or categories associated with the plugin for discovery.
    /// </summary>
    [JsonPropertyName("tags")]
    public ICollection<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the entry assembly name (DLL file name) for the plugin.
    /// </summary>
    [JsonPropertyName("entryAssembly")]
    public string? EntryAssembly { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified type name of the plugin class that implements IPlugin.
    /// </summary>
    [JsonPropertyName("pluginType")]
    public string? PluginType { get; set; }

    /// <summary>
    /// Loads a plugin manifest from a JSON file.
    /// </summary>
    /// <param name="manifestPath">The path to the manifest.json file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The loaded plugin manifest.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the manifest file doesn't exist.</exception>
    /// <exception cref="JsonException">Thrown when the manifest JSON is invalid.</exception>
    public static async Task<PluginManifest> LoadFromFileAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException($"Plugin manifest not found: {manifestPath}", manifestPath);
        }

        using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<PluginManifest>(stream, cancellationToken: cancellationToken)
            ?? throw new JsonException($"Failed to deserialize manifest from {manifestPath}");

        // Validate required fields
        ValidateManifest(manifest);

        return manifest;
    }

    /// <summary>
    /// Loads a plugin manifest from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string containing the manifest data.</param>
    /// <returns>The loaded plugin manifest.</returns>
    /// <exception cref="JsonException">Thrown when the manifest JSON is invalid.</exception>
    public static PluginManifest LoadFromJson(string json)
    {
        var manifest = JsonSerializer.Deserialize<PluginManifest>(json)
            ?? throw new JsonException("Failed to deserialize manifest from JSON string");

        // Validate required fields
        ValidateManifest(manifest);

        return manifest;
    }

    /// <summary>
    /// Saves this manifest to a JSON file.
    /// </summary>
    /// <param name="manifestPath">The path where the manifest should be saved.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task SaveToFileAsync(string manifestPath, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        using var stream = File.Create(manifestPath);
        await JsonSerializer.SerializeAsync(stream, this, options, cancellationToken);
    }

    /// <summary>
    /// Converts this manifest to a JSON string.
    /// </summary>
    /// <returns>The JSON representation of this manifest.</returns>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Converts this manifest to a PluginMetadata instance.
    /// </summary>
    /// <returns>A PluginMetadata instance containing the data from this manifest.</returns>
    public PluginMetadata ToMetadata()
    {
        return new PluginMetadata
        {
            Id = Id,
            Name = Name,
            Version = Version,
            Author = Author,
            Description = Description,
            Website = Website,
            IconPath = IconPath,
            MinApiVersion = MinApiVersion,
            RequiredPermissions = new List<string>(Permissions),
            Tags = new List<string>(Tags)
        };
    }

    /// <summary>
    /// Creates a PluginManifest from a PluginMetadata instance.
    /// </summary>
    /// <param name="metadata">The metadata to convert.</param>
    /// <returns>A new PluginManifest instance.</returns>
    public static PluginManifest FromMetadata(PluginMetadata metadata)
    {
        return new PluginManifest
        {
            Id = metadata.Id,
            Name = metadata.Name,
            Version = metadata.Version,
            Author = metadata.Author,
            Description = metadata.Description,
            Website = metadata.Website,
            IconPath = metadata.IconPath,
            MinApiVersion = metadata.MinApiVersion,
            Permissions = new List<string>(metadata.RequiredPermissions),
            Tags = new List<string>(metadata.Tags)
        };
    }

    /// <summary>
    /// Validates that the manifest contains all required fields.
    /// </summary>
    /// <param name="manifest">The manifest to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing or invalid.</exception>
    private static void ValidateManifest(PluginManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            throw new InvalidOperationException("Plugin manifest must have a non-empty Id");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            throw new InvalidOperationException("Plugin manifest must have a non-empty Name");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidOperationException("Plugin manifest must have a non-empty Version");
        }

        if (string.IsNullOrWhiteSpace(manifest.Author))
        {
            throw new InvalidOperationException("Plugin manifest must have a non-empty Author");
        }

        // Validate version format (basic semantic versioning check)
        if (!IsValidSemanticVersion(manifest.Version))
        {
            throw new InvalidOperationException($"Plugin version '{manifest.Version}' is not a valid semantic version (e.g., 1.0.0)");
        }

        // Validate MinApiVersion format if provided
        if (!string.IsNullOrWhiteSpace(manifest.MinApiVersion) && !IsValidSemanticVersion(manifest.MinApiVersion))
        {
            throw new InvalidOperationException($"Minimum API version '{manifest.MinApiVersion}' is not a valid semantic version");
        }
    }

    /// <summary>
    /// Validates that a string is a valid semantic version (major.minor.patch).
    /// </summary>
    private static bool IsValidSemanticVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;

        var parts = version.Split('.');
        if (parts.Length < 2 || parts.Length > 3)
            return false;

        return parts.All(part => int.TryParse(part, out _));
    }
}
