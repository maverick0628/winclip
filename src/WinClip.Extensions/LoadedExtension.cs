using System.Runtime.Loader;
using WinClip.Actions;

namespace WinClip.Extensions;

/// <summary>
/// Represents a loaded and initialized extension with its manifest and actions.
/// </summary>
public sealed class LoadedExtension : IDisposable
{
    private readonly AssemblyLoadContext? _loadContext;

    public ExtensionManifest Manifest { get; }
    public IExtension? Instance { get; }
    public IReadOnlyList<IAction> Actions { get; }
    public bool IsEnabled { get; set; }
    public string? LoadError { get; }

    /// <summary>Directory on disk containing this extension's files.</summary>
    public string Directory { get; }

    public LoadedExtension(
        ExtensionManifest manifest,
        IExtension? instance,
        IReadOnlyList<IAction> actions,
        string directory,
        AssemblyLoadContext? loadContext = null,
        bool isEnabled = true,
        string? loadError = null)
    {
        Manifest = manifest;
        Instance = instance;
        Actions = actions;
        Directory = directory;
        _loadContext = loadContext;
        IsEnabled = isEnabled;
        LoadError = loadError;
    }

    /// <summary>Whether this extension loaded successfully (no error, has actions).</summary>
    public bool IsLoaded => LoadError == null && Actions.Count > 0;

    public void Dispose()
    {
        Instance?.Dispose();
        _loadContext?.Unload();
    }
}
