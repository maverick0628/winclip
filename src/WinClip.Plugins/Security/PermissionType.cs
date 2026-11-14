namespace WinClip.Plugins.Security;

/// <summary>
/// Defines the types of permissions that can be requested by plugins.
/// Permissions control access to sensitive WinClip APIs and system resources.
/// </summary>
[Flags]
public enum PermissionType
{
    /// <summary>
    /// No permissions. Used for plugins that require no special access.
    /// </summary>
    None = 0,

    /// <summary>
    /// Permission to read from and write to the system clipboard.
    /// Required for plugins that need to access clipboard content beyond what's
    /// provided in the action context.
    /// </summary>
    Clipboard = 1 << 0,

    /// <summary>
    /// Permission to read and write files outside the plugin's data directory.
    /// Required for plugins that need access to arbitrary file system locations.
    /// Without this permission, plugins can only access their own data directory.
    /// </summary>
    FileSystem = 1 << 1,

    /// <summary>
    /// Permission to make network requests to external services.
    /// Required for plugins that need to communicate with web APIs or
    /// download/upload data over the network.
    /// </summary>
    Network = 1 << 2,

    /// <summary>
    /// Permission to launch external processes or applications.
    /// Required for plugins that need to spawn executables, scripts,
    /// or interact with other programs.
    /// </summary>
    Process = 1 << 3,

    /// <summary>
    /// Permission to store and retrieve data using the plugin storage API.
    /// Required for plugins that need persistent key-value storage or file storage
    /// within their data directory.
    /// </summary>
    Storage = 1 << 4,

    /// <summary>
    /// Permission to display notifications to the user.
    /// Required for plugins that need to show toast notifications or
    /// progress updates in the system notification area.
    /// </summary>
    Notifications = 1 << 5,

    /// <summary>
    /// All available permissions. Used for highly privileged plugins
    /// that require full access to system resources.
    /// </summary>
    All = Clipboard | FileSystem | Network | Process | Storage | Notifications
}
