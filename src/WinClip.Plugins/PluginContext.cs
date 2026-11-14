using WinClip.Plugins.API;
using WinClip.Plugins.Security;

namespace WinClip.Plugins;

/// <summary>
/// Provides plugins with access to WinClip APIs and granted permissions.
/// This context is passed to plugins during initialization and provides
/// the interface for interacting with WinClip functionality.
/// </summary>
public class PluginContext
{
    /// <summary>
    /// Gets the permissions that have been granted to the plugin.
    /// This determines which APIs and operations the plugin is authorized to use.
    /// </summary>
    public PermissionType GrantedPermissions { get; }

    /// <summary>
    /// Gets the clipboard API instance for accessing system clipboard functionality.
    /// This property will be null if the plugin has not been granted Clipboard permission.
    /// </summary>
    public IClipboardApi? Clipboard { get; internal set; }

    /// <summary>
    /// Gets the storage API instance for persisting plugin data.
    /// This property will be null if the plugin has not been granted Storage permission.
    /// </summary>
    public IStorageApi? Storage { get; internal set; }

    /// <summary>
    /// Gets the notification API instance for displaying user notifications.
    /// This property will be null if the plugin has not been granted Notifications permission.
    /// </summary>
    public INotificationApi? Notifications { get; internal set; }

    /// <summary>
    /// Gets the unique identifier of the plugin this context belongs to.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginContext"/> class.
    /// </summary>
    /// <param name="pluginId">The unique identifier of the plugin.</param>
    /// <param name="grantedPermissions">The permissions granted to the plugin.</param>
    /// <exception cref="ArgumentNullException">Thrown when pluginId is null or whitespace.</exception>
    public PluginContext(string pluginId, PermissionType grantedPermissions)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            throw new ArgumentNullException(nameof(pluginId), "Plugin ID cannot be null or empty.");
        }

        PluginId = pluginId;
        GrantedPermissions = grantedPermissions;
    }

    /// <summary>
    /// Checks if the plugin has been granted a specific permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the permission has been granted; false otherwise.</returns>
    public bool HasPermission(PermissionType permission)
    {
        return PermissionGuard.HasPermission(GrantedPermissions, permission);
    }

    /// <summary>
    /// Checks if the plugin has been granted all of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if all permissions have been granted; false otherwise.</returns>
    public bool HasAllPermissions(PermissionType permissions)
    {
        return PermissionGuard.HasAllPermissions(GrantedPermissions, permissions);
    }

    /// <summary>
    /// Checks if the plugin has been granted any of the specified permissions.
    /// </summary>
    /// <param name="permissions">The permissions to check.</param>
    /// <returns>True if at least one permission has been granted; false otherwise.</returns>
    public bool HasAnyPermission(PermissionType permissions)
    {
        return PermissionGuard.HasAnyPermission(GrantedPermissions, permissions);
    }
}
