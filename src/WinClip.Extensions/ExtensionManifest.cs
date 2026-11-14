using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinClip.Extensions;

/// <summary>
/// Deserialized from plugin.json in each extension directory.
/// </summary>
public sealed class ExtensionManifest
{
    /// <summary>Reverse-domain identifier (e.g. "com.winclip.texttransform").</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>SemVer version string.</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.0.0";

    /// <summary>Author name.</summary>
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>Short description.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Project/homepage URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Minimum WinClip version required.</summary>
    [JsonPropertyName("minWinClipVersion")]
    public string MinWinClipVersion { get; set; } = "1.0.0";

    /// <summary>Name of the DLL to load (relative to the extension directory).</summary>
    [JsonPropertyName("entryAssembly")]
    public string EntryAssembly { get; set; } = string.Empty;

    /// <summary>Actions provided by this extension.</summary>
    [JsonPropertyName("actions")]
    public List<ActionManifest> Actions { get; set; } = new();

    /// <summary>
    /// Legacy field kept so existing plugin.json files still load. It has no effect:
    /// every extension and action is available to every user.
    /// </summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "free";

    /// <summary>Whether this is a built-in extension (shipped with WinClip).</summary>
    [JsonPropertyName("builtIn")]
    public bool BuiltIn { get; set; }

    /// <summary>
    /// Loads a manifest from a plugin.json file path.
    /// </summary>
    public static ExtensionManifest LoadFrom(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ExtensionManifest>(json)
            ?? throw new InvalidOperationException($"Failed to deserialize manifest: {path}");
    }

    /// <summary>
    /// Validates that required fields are present.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Id)
            && !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(EntryAssembly)
            && Actions.Count > 0;
    }
}

/// <summary>
/// Describes a single action within an extension manifest.
/// </summary>
public sealed class ActionManifest
{
    /// <summary>Full type name of the IAction implementation (e.g. "MyExt.MyAction").</summary>
    [JsonPropertyName("typeName")]
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Display name shown in the popup.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Icon name (maps to a built-in icon set).</summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "default";

    /// <summary>Legacy field kept for manifest compatibility. It has no effect.</summary>
    [JsonPropertyName("tier")]
    public string? Tier { get; set; }
}
