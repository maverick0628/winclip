using System.Reflection;

namespace WinClip.Extensions;

/// <summary>
/// Represents metadata information about a loaded WinClip extension.
/// </summary>
public class ExtensionMetadata
{
    /// <summary>
    /// Gets the unique identifier for the extension.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the extension.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the extension.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the author of the extension.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Gets a brief description of what the extension does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the file path to the extension DLL.
    /// </summary>
    public string DllPath { get; }

    /// <summary>
    /// Gets the loaded assembly containing the extension.
    /// </summary>
    public Assembly? LoadedAssembly { get; }

    /// <summary>
    /// Gets the settings for the extension.
    /// </summary>
    public ExtensionSettings? Settings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionMetadata"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the extension.</param>
    /// <param name="name">The display name of the extension.</param>
    /// <param name="version">The version of the extension.</param>
    /// <param name="author">The author of the extension.</param>
    /// <param name="description">A brief description of what the extension does.</param>
    /// <param name="dllPath">The file path to the extension DLL.</param>
    /// <param name="loadedAssembly">The loaded assembly containing the extension.</param>
    /// <param name="settings">The settings for the extension.</param>
    public ExtensionMetadata(
        string id,
        string name,
        string version,
        string author,
        string description,
        string dllPath,
        Assembly? loadedAssembly = null,
        ExtensionSettings? settings = null)
    {
        Id = id;
        Name = name;
        Version = version;
        Author = author;
        Description = description;
        DllPath = dllPath;
        LoadedAssembly = loadedAssembly;
        Settings = settings;
    }

    /// <summary>
    /// Creates extension metadata from an IWinClipExtension instance.
    /// </summary>
    /// <param name="extension">The extension instance to extract metadata from.</param>
    /// <param name="dllPath">The file path to the extension DLL.</param>
    /// <param name="loadedAssembly">The loaded assembly containing the extension.</param>
    /// <param name="settings">The settings for the extension.</param>
    /// <returns>A new <see cref="ExtensionMetadata"/> instance.</returns>
    public static ExtensionMetadata FromExtension(
        IWinClipExtension extension,
        string dllPath,
        Assembly? loadedAssembly = null,
        ExtensionSettings? settings = null)
    {
        return new ExtensionMetadata(
            extension.Id,
            extension.Name,
            extension.Version,
            extension.Author,
            extension.Description,
            dllPath,
            loadedAssembly,
            settings);
    }
}
