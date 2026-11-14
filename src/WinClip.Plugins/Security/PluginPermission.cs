namespace WinClip.Plugins.Security;

/// <summary>
/// Provides metadata and utilities for working with plugin permissions.
/// This class helps manage permission requests, validation, and display.
/// </summary>
public static class PluginPermission
{
    /// <summary>
    /// String constant for clipboard permission.
    /// </summary>
    public const string ClipboardPermission = "clipboard";

    /// <summary>
    /// String constant for file system permission.
    /// </summary>
    public const string FileSystemPermission = "filesystem";

    /// <summary>
    /// String constant for network permission.
    /// </summary>
    public const string NetworkPermission = "network";

    /// <summary>
    /// String constant for process permission.
    /// </summary>
    public const string ProcessPermission = "process";

    /// <summary>
    /// String constant for storage permission.
    /// </summary>
    public const string StoragePermission = "storage";

    /// <summary>
    /// String constant for notifications permission.
    /// </summary>
    public const string NotificationsPermission = "notifications";

    /// <summary>
    /// Converts a permission type to its string representation.
    /// </summary>
    /// <param name="permission">The permission type to convert.</param>
    /// <returns>The string representation of the permission.</returns>
    /// <exception cref="ArgumentException">Thrown when the permission type is invalid or represents multiple permissions.</exception>
    public static string ToString(PermissionType permission)
    {
        return permission switch
        {
            PermissionType.None => string.Empty,
            PermissionType.Clipboard => ClipboardPermission,
            PermissionType.FileSystem => FileSystemPermission,
            PermissionType.Network => NetworkPermission,
            PermissionType.Process => ProcessPermission,
            PermissionType.Storage => StoragePermission,
            PermissionType.Notifications => NotificationsPermission,
            _ => throw new ArgumentException($"Cannot convert permission '{permission}' to string. Use ToStringList for combined permissions.", nameof(permission))
        };
    }

    /// <summary>
    /// Converts a collection of permission types to their string representations.
    /// </summary>
    /// <param name="permissions">The permission flags to convert.</param>
    /// <returns>A list of string representations of the permissions.</returns>
    public static IReadOnlyList<string> ToStringList(PermissionType permissions)
    {
        var result = new List<string>();

        if (permissions == PermissionType.None)
            return result;

        if (permissions.HasFlag(PermissionType.Clipboard))
            result.Add(ClipboardPermission);

        if (permissions.HasFlag(PermissionType.FileSystem))
            result.Add(FileSystemPermission);

        if (permissions.HasFlag(PermissionType.Network))
            result.Add(NetworkPermission);

        if (permissions.HasFlag(PermissionType.Process))
            result.Add(ProcessPermission);

        if (permissions.HasFlag(PermissionType.Storage))
            result.Add(StoragePermission);

        if (permissions.HasFlag(PermissionType.Notifications))
            result.Add(NotificationsPermission);

        return result;
    }

    /// <summary>
    /// Converts a string permission to its enum representation.
    /// </summary>
    /// <param name="permission">The permission string to convert.</param>
    /// <returns>The corresponding PermissionType value.</returns>
    /// <exception cref="ArgumentException">Thrown when the permission string is not recognized.</exception>
    public static PermissionType FromString(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return PermissionType.None;

        return permission.ToLowerInvariant() switch
        {
            ClipboardPermission => PermissionType.Clipboard,
            FileSystemPermission => PermissionType.FileSystem,
            NetworkPermission => PermissionType.Network,
            ProcessPermission => PermissionType.Process,
            StoragePermission => PermissionType.Storage,
            NotificationsPermission => PermissionType.Notifications,
            _ => throw new ArgumentException($"Unknown permission: '{permission}'", nameof(permission))
        };
    }

    /// <summary>
    /// Converts a collection of permission strings to a combined permission type flags value.
    /// </summary>
    /// <param name="permissions">The collection of permission strings.</param>
    /// <returns>The combined PermissionType flags.</returns>
    /// <exception cref="ArgumentException">Thrown when any permission string is not recognized.</exception>
    public static PermissionType FromStringList(IEnumerable<string> permissions)
    {
        if (permissions == null)
            return PermissionType.None;

        var result = PermissionType.None;

        foreach (var permission in permissions)
        {
            if (!string.IsNullOrWhiteSpace(permission))
            {
                result |= FromString(permission);
            }
        }

        return result;
    }

    /// <summary>
    /// Tries to convert a string permission to its enum representation without throwing an exception.
    /// </summary>
    /// <param name="permission">The permission string to convert.</param>
    /// <param name="result">The resulting PermissionType if successful.</param>
    /// <returns>True if the conversion succeeded; false otherwise.</returns>
    public static bool TryFromString(string permission, out PermissionType result)
    {
        result = PermissionType.None;

        if (string.IsNullOrWhiteSpace(permission))
            return true;

        try
        {
            result = FromString(permission);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets a human-readable display name for a permission.
    /// </summary>
    /// <param name="permission">The permission type.</param>
    /// <returns>The display name for the permission.</returns>
    public static string GetDisplayName(PermissionType permission)
    {
        return permission switch
        {
            PermissionType.None => "No Permissions",
            PermissionType.Clipboard => "Clipboard Access",
            PermissionType.FileSystem => "File System Access",
            PermissionType.Network => "Network Access",
            PermissionType.Process => "Process Execution",
            PermissionType.Storage => "Data Storage",
            PermissionType.Notifications => "Notifications",
            PermissionType.All => "All Permissions",
            _ => permission.ToString()
        };
    }

    /// <summary>
    /// Gets a detailed description of what a permission allows.
    /// </summary>
    /// <param name="permission">The permission type.</param>
    /// <returns>A description of the permission's capabilities.</returns>
    public static string GetDescription(PermissionType permission)
    {
        return permission switch
        {
            PermissionType.None => "This plugin requires no special permissions.",
            PermissionType.Clipboard => "Allows the plugin to read from and write to the system clipboard.",
            PermissionType.FileSystem => "Allows the plugin to read and write files anywhere on your computer.",
            PermissionType.Network => "Allows the plugin to make network requests to external services.",
            PermissionType.Process => "Allows the plugin to launch external programs and processes.",
            PermissionType.Storage => "Allows the plugin to store data persistently on your computer.",
            PermissionType.Notifications => "Allows the plugin to display notifications and alerts.",
            PermissionType.All => "Grants the plugin full access to all system resources.",
            _ => "Multiple permissions requested."
        };
    }

    /// <summary>
    /// Gets the risk level associated with a permission.
    /// Used to inform users about potentially dangerous permissions.
    /// </summary>
    /// <param name="permission">The permission type.</param>
    /// <returns>The risk level (Low, Medium, or High).</returns>
    public static RiskLevel GetRiskLevel(PermissionType permission)
    {
        return permission switch
        {
            PermissionType.None => RiskLevel.None,
            PermissionType.Storage => RiskLevel.Low,
            PermissionType.Notifications => RiskLevel.Low,
            PermissionType.Clipboard => RiskLevel.Medium,
            PermissionType.Network => RiskLevel.Medium,
            PermissionType.FileSystem => RiskLevel.High,
            PermissionType.Process => RiskLevel.High,
            PermissionType.All => RiskLevel.High,
            _ => RiskLevel.Medium
        };
    }

    /// <summary>
    /// Validates that all permission strings in a collection are recognized.
    /// </summary>
    /// <param name="permissions">The collection of permission strings to validate.</param>
    /// <returns>True if all permissions are valid; false otherwise.</returns>
    public static bool ValidatePermissions(IEnumerable<string> permissions)
    {
        if (permissions == null)
            return true;

        foreach (var permission in permissions)
        {
            if (!string.IsNullOrWhiteSpace(permission) && !TryFromString(permission, out _))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets a list of all available permission strings.
    /// </summary>
    /// <returns>A read-only list of all permission strings.</returns>
    public static IReadOnlyList<string> GetAllPermissions()
    {
        return new[]
        {
            ClipboardPermission,
            FileSystemPermission,
            NetworkPermission,
            ProcessPermission,
            StoragePermission,
            NotificationsPermission
        };
    }

    /// <summary>
    /// Checks if one set of permissions is a subset of another.
    /// </summary>
    /// <param name="required">The required permissions.</param>
    /// <param name="granted">The granted permissions.</param>
    /// <returns>True if all required permissions are included in granted permissions.</returns>
    public static bool IsSubsetOf(PermissionType required, PermissionType granted)
    {
        return (required & granted) == required;
    }

    /// <summary>
    /// Checks if a collection of permission strings contains a specific permission.
    /// </summary>
    /// <param name="permissions">The collection of permission strings.</param>
    /// <param name="permission">The permission to check for.</param>
    /// <returns>True if the permission is in the collection; false otherwise.</returns>
    public static bool Contains(IEnumerable<string> permissions, string permission)
    {
        if (permissions == null || string.IsNullOrWhiteSpace(permission))
            return false;

        return permissions.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Represents the risk level associated with a permission.
/// Used to inform users about the potential security implications of granting a permission.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// No risk - no permissions required.
    /// </summary>
    None,

    /// <summary>
    /// Low risk - permission allows limited access to user data or system resources.
    /// </summary>
    Low,

    /// <summary>
    /// Medium risk - permission allows access to potentially sensitive data.
    /// </summary>
    Medium,

    /// <summary>
    /// High risk - permission allows broad access to system resources and could be dangerous if misused.
    /// </summary>
    High
}
