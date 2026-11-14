using WinClip.Plugins.Security;

namespace WinClip.Plugins;

/// <summary>
/// Defines the contract for plugin actions that can be executed by WinClip.
/// Plugin actions are triggered by user interactions and can process clipboard content,
/// files, images, and other data based on granted permissions.
/// </summary>
public interface IPluginAction
{
    /// <summary>
    /// Gets the unique identifier for this action.
    /// This ID is used to reference the action in configuration and execution contexts.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the action shown to users in the UI.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this action does.
    /// This is displayed to users to help them understand the action's purpose.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the plugin action with the provided context.
    /// This method is called when the user triggers the action.
    /// </summary>
    /// <param name="context">The action context containing input data and plugin permissions.</param>
    /// <returns>A task that represents the asynchronous execution of the action.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the plugin attempts to access resources without the required permissions.
    /// For example, accessing Files without FileSystem permission.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    Task ExecuteAsync(ActionContext context);
}

/// <summary>
/// Provides context and data to plugin actions during execution.
/// This class contains input data (clipboard content, files, images) and access to
/// the plugin's permissions and APIs through the PluginContext.
/// </summary>
public class ActionContext
{
    /// <summary>
    /// Gets the plugin context that provides access to WinClip APIs and permission information.
    /// Use this to check permissions before accessing sensitive resources.
    /// </summary>
    public PluginContext PluginContext { get; }

    /// <summary>
    /// Gets the current clipboard text content, if available.
    /// This property may be null if no text content is on the clipboard.
    /// </summary>
    public string? ClipboardText { get; init; }

    /// <summary>
    /// Gets the file paths associated with this action.
    /// IMPORTANT: Access to file paths requires the FileSystem permission.
    /// Plugins without FileSystem permission will receive an empty collection,
    /// or an exception may be thrown during action execution if file access is attempted.
    /// </summary>
    /// <remarks>
    /// The FileSystem permission (<see cref="PermissionType.FileSystem"/>) must be granted
    /// to the plugin for this property to contain file paths. Without this permission,
    /// plugins cannot access files outside their designated data directory.
    /// Use <see cref="PluginContext.HasPermission(PermissionType)"/> to check for
    /// FileSystem permission before attempting to access files.
    /// </remarks>
    public IReadOnlyList<string> Files { get; init; }

    /// <summary>
    /// Gets the image data associated with this action, if available.
    /// This property may be null if no image content is available.
    /// </summary>
    /// <remarks>
    /// Image data is typically provided when clipboard contains an image
    /// or when the action is triggered on image files.
    /// </remarks>
    public byte[]? ImageData { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionContext"/> class.
    /// </summary>
    /// <param name="pluginContext">The plugin context providing access to permissions and APIs.</param>
    /// <exception cref="ArgumentNullException">Thrown when pluginContext is null.</exception>
    public ActionContext(PluginContext pluginContext)
    {
        PluginContext = pluginContext ?? throw new ArgumentNullException(nameof(pluginContext));
        Files = Array.Empty<string>();
    }

    /// <summary>
    /// Validates that the plugin has the required permission to access files.
    /// This method should be called before providing file paths to plugins.
    /// </summary>
    /// <returns>True if the plugin has FileSystem permission; false otherwise.</returns>
    public bool CanAccessFiles()
    {
        return PluginContext.HasPermission(PermissionType.FileSystem);
    }

    /// <summary>
    /// Gets the files if the plugin has permission, or an empty collection otherwise.
    /// This is a safe accessor that enforces permission checks.
    /// </summary>
    /// <returns>The file paths if FileSystem permission is granted; otherwise an empty collection.</returns>
    public IReadOnlyList<string> GetAccessibleFiles()
    {
        return CanAccessFiles() ? Files : Array.Empty<string>();
    }
}
