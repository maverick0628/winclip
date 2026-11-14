using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WinClip.Actions;
using WinClip.Core.Services;

namespace WinClip.Extensions;

/// <summary>
/// Central coordinator for the extension system.
/// Manages discovery, loading, and enabling/disabling of extensions.
/// </summary>
public class PluginManager : IDisposable
{
    private readonly PluginLoader _loader;
    private readonly IActionRegistry _actionRegistry;
    private readonly ILogger<PluginManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly List<LoadedExtension> _extensions = new();

    public PluginManager(
        PluginLoader loader,
        IActionRegistry actionRegistry,
        ILogger<PluginManager> logger,
        ILoggerFactory loggerFactory,
        IConfiguration configuration)
    {
        _loader = loader;
        _actionRegistry = actionRegistry;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _configuration = configuration;
    }

    /// <summary>All discovered extensions (loaded or not).</summary>
    public IReadOnlyList<LoadedExtension> Extensions => _extensions;

    /// <summary>
    /// Discovers and loads all extensions from the given directory.
    /// </summary>
    public void LoadExtensionsFrom(string extensionsDirectory)
    {
        var manifests = _loader.DiscoverExtensions(extensionsDirectory);

        foreach (var manifest in manifests)
        {
            var extDir = Path.Combine(extensionsDirectory, manifest.Id);
            if (!Directory.Exists(extDir))
            {
                extDir = Directory.GetDirectories(extensionsDirectory)
                    .FirstOrDefault(d => File.Exists(Path.Combine(d, "plugin.json"))
                        && ExtensionManifest.LoadFrom(Path.Combine(d, "plugin.json")).Id == manifest.Id)
                    ?? extDir;
            }

            var context = CreateContext(manifest);
            var loaded = _loader.LoadExtension(manifest, extDir, context);

            _extensions.Add(loaded);

            if (loaded.IsEnabled && loaded.IsLoaded)
            {
                foreach (var action in loaded.Actions)
                {
                    try
                    {
                        _actionRegistry.RegisterAction(action);
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogWarning("Action {Name} already registered, skipping", action.Name);
                    }
                }
            }
        }

        _logger.LogInformation("Loaded {Count} extensions ({Active} active)",
            _extensions.Count, _extensions.Count(e => e.IsEnabled && e.IsLoaded));
    }

    /// <summary>
    /// Registers a built-in action directly (not from an extension assembly).
    /// </summary>
    public void RegisterBuiltInAction(IAction action)
    {
        _actionRegistry.RegisterAction(action);
    }

    /// <summary>
    /// Enables an extension by ID.
    /// </summary>
    public bool EnableExtension(string extensionId)
    {
        var ext = _extensions.FirstOrDefault(e => e.Manifest.Id == extensionId);
        if (ext == null) return false;

        ext.IsEnabled = true;
        foreach (var action in ext.Actions)
        {
            try { _actionRegistry.RegisterAction(action); }
            catch (InvalidOperationException) { }
        }

        return true;
    }

    /// <summary>
    /// Disables an extension by ID.
    /// </summary>
    public bool DisableExtension(string extensionId)
    {
        var ext = _extensions.FirstOrDefault(e => e.Manifest.Id == extensionId);
        if (ext == null) return false;

        ext.IsEnabled = false;
        foreach (var action in ext.Actions)
        {
            _actionRegistry.UnregisterAction(action.Name);
        }

        return true;
    }

    private ExtensionContext CreateContext(ExtensionManifest manifest)
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WinClip", "extensions", manifest.Id, "data");

        Directory.CreateDirectory(dataDir);

        var logger = _loggerFactory.CreateLogger(manifest.Id);

        return new ExtensionContext(logger, dataDir, _configuration);
    }

    public void Dispose()
    {
        foreach (var ext in _extensions)
            ext.Dispose();
        _extensions.Clear();
    }
}
