namespace WinClip.Plugins;

/// <summary>
/// Defines the contract for WinClip plugins. All plugins must implement this interface
/// to integrate with the WinClip plugin system.
/// </summary>
public interface IPlugin : IDisposable
{
    /// <summary>
    /// Gets the metadata describing this plugin.
    /// </summary>
    PluginMetadata Metadata { get; }

    /// <summary>
    /// Called when the plugin is first loaded into memory. This is the plugin's opportunity
    /// to validate its environment and dependencies before initialization.
    /// </summary>
    /// <param name="context">The plugin context providing access to WinClip APIs.</param>
    /// <returns>True if the plugin loaded successfully; false otherwise.</returns>
    bool Load(PluginContext context);

    /// <summary>
    /// Called to initialize the plugin after it has been loaded. The plugin should
    /// set up any necessary state and prepare to handle action requests.
    /// </summary>
    /// <returns>A task that completes when initialization is finished.</returns>
    Task InitializeAsync();

    /// <summary>
    /// Gets the collection of actions provided by this plugin. These actions will
    /// appear in the WinClip popup when applicable.
    /// </summary>
    /// <returns>A collection of plugin actions.</returns>
    IEnumerable<IPluginAction> GetActions();

    /// <summary>
    /// Called when the plugin is being unloaded. The plugin should clean up any
    /// resources, cancel ongoing operations, and prepare for termination.
    /// </summary>
    /// <returns>A task that completes when cleanup is finished.</returns>
    Task UnloadAsync();
}
