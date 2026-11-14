using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using WinClip.Actions;

namespace WinClip.Extensions;

/// <summary>
/// Discovers and loads extension assemblies from the extensions directory.
/// Each extension gets its own AssemblyLoadContext for isolation.
/// </summary>
public class PluginLoader
{
    private readonly ILogger _logger;

    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Discovers all extensions in the given directory.
    /// Each subdirectory is expected to contain a plugin.json manifest.
    /// </summary>
    public IReadOnlyList<ExtensionManifest> DiscoverExtensions(string extensionsDirectory)
    {
        var manifests = new List<ExtensionManifest>();

        if (!Directory.Exists(extensionsDirectory))
        {
            _logger.LogDebug("Extensions directory does not exist: {Dir}", extensionsDirectory);
            return manifests;
        }

        foreach (var dir in Directory.GetDirectories(extensionsDirectory))
        {
            var manifestPath = Path.Combine(dir, "plugin.json");
            if (!File.Exists(manifestPath))
            {
                _logger.LogDebug("No plugin.json in {Dir}, skipping", dir);
                continue;
            }

            try
            {
                var manifest = ExtensionManifest.LoadFrom(manifestPath);
                if (manifest.IsValid())
                {
                    manifests.Add(manifest);
                    _logger.LogInformation("Discovered extension: {Name} ({Id}) v{Version}",
                        manifest.Name, manifest.Id, manifest.Version);
                }
                else
                {
                    _logger.LogWarning("Invalid manifest in {Dir}: missing required fields", dir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read manifest from {Path}", manifestPath);
            }
        }

        return manifests;
    }

    /// <summary>
    /// Loads an extension assembly and creates its IAction instances.
    /// Returns a LoadedExtension (with error info if loading failed).
    /// </summary>
    public LoadedExtension LoadExtension(ExtensionManifest manifest, string extensionDirectory, ExtensionContext context)
    {
        var assemblyPath = Path.Combine(extensionDirectory, manifest.EntryAssembly);

        if (!File.Exists(assemblyPath))
        {
            var error = $"Entry assembly not found: {assemblyPath}";
            _logger.LogError(error);
            return new LoadedExtension(manifest, null, Array.Empty<IAction>(), extensionDirectory, loadError: error);
        }

        try
        {
            // Load into an isolated context so extensions don't pollute each other
            var loadContext = new AssemblyLoadContext(manifest.Id, isCollectible: true);
            var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));

            // Find and instantiate IExtension if present
            IExtension? extensionInstance = null;
            var extensionType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IExtension).IsAssignableFrom(t) && !t.IsAbstract);
            if (extensionType != null)
            {
                extensionInstance = (IExtension?)Activator.CreateInstance(extensionType);
                if (extensionInstance != null)
                    Task.Run(() => extensionInstance.InitializeAsync(context)).GetAwaiter().GetResult();
            }

            // Create IAction instances declared in manifest
            var actions = new List<IAction>();
            foreach (var actionManifest in manifest.Actions)
            {
                var actionType = assembly.GetType(actionManifest.TypeName);
                if (actionType == null)
                {
                    _logger.LogWarning("Action type {Type} not found in {Assembly}",
                        actionManifest.TypeName, manifest.EntryAssembly);
                    continue;
                }

                if (!typeof(IAction).IsAssignableFrom(actionType))
                {
                    _logger.LogWarning("Type {Type} does not implement IAction", actionManifest.TypeName);
                    continue;
                }

                var action = (IAction?)Activator.CreateInstance(actionType);
                if (action != null)
                {
                    actions.Add(action);
                    _logger.LogInformation("Loaded action: {Name} from {Extension}",
                        action.Name, manifest.Name);
                }
            }

            return new LoadedExtension(manifest, extensionInstance, actions, extensionDirectory, loadContext: loadContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load extension {Id}", manifest.Id);
            return new LoadedExtension(manifest, null, Array.Empty<IAction>(), extensionDirectory,
                loadError: ex.Message);
        }
    }
}
