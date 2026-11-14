namespace WinClip.Plugins.Security;

/// <summary>
/// Provides runtime permission validation and enforcement for plugin operations.
/// This class ensures that plugins only access resources and APIs for which they have been granted permissions.
/// </summary>
public static class PermissionGuard
{
    /// <summary>
    /// Checks if a set of granted permissions includes a required permission.
    /// </summary>
    /// <param name="grantedPermissions">The permissions that have been granted to the plugin.</param>
    /// <param name="requiredPermission">The permission required to perform an operation.</param>
    /// <returns>True if the required permission is included in the granted permissions; false otherwise.</returns>
    public static bool HasPermission(PermissionType grantedPermissions, PermissionType requiredPermission)
    {
        // Check if the required permission is set in the granted permissions using bitwise AND
        return (grantedPermissions & requiredPermission) == requiredPermission;
    }

    /// <summary>
    /// Enforces that a plugin has the required permission to perform an operation.
    /// Throws an exception if the permission is not granted.
    /// </summary>
    /// <param name="grantedPermissions">The permissions that have been granted to the plugin.</param>
    /// <param name="requiredPermission">The permission required to perform an operation.</param>
    /// <param name="operationName">A human-readable name for the operation being performed (e.g., "Clipboard Access").</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the required permission is not granted.</exception>
    public static void RequirePermission(PermissionType grantedPermissions, PermissionType requiredPermission, string operationName)
    {
        if (!HasPermission(grantedPermissions, requiredPermission))
        {
            var permissionName = PluginPermission.GetDisplayName(requiredPermission);
            throw new UnauthorizedAccessException(
                $"Access denied: '{operationName}' requires the '{permissionName}' permission. " +
                $"This plugin has not been granted this permission.");
        }
    }

    /// <summary>
    /// Checks if a set of granted permissions includes all of the required permissions.
    /// Useful when an operation requires multiple permissions.
    /// </summary>
    /// <param name="grantedPermissions">The permissions that have been granted to the plugin.</param>
    /// <param name="requiredPermissions">The permissions required to perform an operation.</param>
    /// <returns>True if all required permissions are included in the granted permissions; false otherwise.</returns>
    public static bool HasAllPermissions(PermissionType grantedPermissions, PermissionType requiredPermissions)
    {
        return (grantedPermissions & requiredPermissions) == requiredPermissions;
    }

    /// <summary>
    /// Enforces that a plugin has all of the required permissions to perform an operation.
    /// Throws an exception if any of the required permissions are not granted.
    /// </summary>
    /// <param name="grantedPermissions">The permissions that have been granted to the plugin.</param>
    /// <param name="requiredPermissions">The permissions required to perform an operation.</param>
    /// <param name="operationName">A human-readable name for the operation being performed.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when any of the required permissions are not granted.</exception>
    public static void RequireAllPermissions(PermissionType grantedPermissions, PermissionType requiredPermissions, string operationName)
    {
        if (!HasAllPermissions(grantedPermissions, requiredPermissions))
        {
            var missingPermissions = GetMissingPermissions(grantedPermissions, requiredPermissions);
            var missingPermissionNames = string.Join(", ", missingPermissions.Select(p => $"'{PluginPermission.GetDisplayName(p)}'"));

            throw new UnauthorizedAccessException(
                $"Access denied: '{operationName}' requires the following permission(s): {missingPermissionNames}. " +
                $"This plugin has not been granted these permissions.");
        }
    }

    /// <summary>
    /// Checks if a set of granted permissions includes at least one of the required permissions.
    /// Useful when an operation can be performed with any of several permissions.
    /// </summary>
    /// <param name="grantedPermissions">The permissions that have been granted to the plugin.</param>
    /// <param name="requiredPermissions">The permissions that could allow the operation.</param>
    /// <returns>True if at least one of the required permissions is included in the granted permissions; false otherwise.</returns>
    public static bool HasAnyPermission(PermissionType grantedPermissions, PermissionType requiredPermissions)
    {
        return (grantedPermissions & requiredPermissions) != PermissionType.None;
    }

    /// <summary>
    /// Enforces that a plugin has at least one of the required permissions to perform an operation.
    /// Throws an exception if none of the required permissions are granted.
    /// </summary>
    /// <param name="grantedPermissions">The permissions that have been granted to the plugin.</param>
    /// <param name="requiredPermissions">The permissions that could allow the operation.</param>
    /// <param name="operationName">A human-readable name for the operation being performed.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when none of the required permissions are granted.</exception>
    public static void RequireAnyPermission(PermissionType grantedPermissions, PermissionType requiredPermissions, string operationName)
    {
        if (!HasAnyPermission(grantedPermissions, requiredPermissions))
        {
            var permissionsList = GetIndividualPermissions(requiredPermissions);
            var permissionNames = string.Join(" or ", permissionsList.Select(p => $"'{PluginPermission.GetDisplayName(p)}'"));

            throw new UnauthorizedAccessException(
                $"Access denied: '{operationName}' requires at least one of the following permissions: {permissionNames}. " +
                $"This plugin has not been granted any of these permissions.");
        }
    }

    /// <summary>
    /// Gets a list of permissions that are required but not granted.
    /// </summary>
    /// <param name="grantedPermissions">The permissions that have been granted.</param>
    /// <param name="requiredPermissions">The permissions that are required.</param>
    /// <returns>A list of missing permissions.</returns>
    private static List<PermissionType> GetMissingPermissions(PermissionType grantedPermissions, PermissionType requiredPermissions)
    {
        var missing = new List<PermissionType>();
        var individual = GetIndividualPermissions(requiredPermissions);

        foreach (var permission in individual)
        {
            if (!HasPermission(grantedPermissions, permission))
            {
                missing.Add(permission);
            }
        }

        return missing;
    }

    /// <summary>
    /// Breaks down a combined permission flags value into individual permission values.
    /// </summary>
    /// <param name="permissions">The combined permissions flags.</param>
    /// <returns>A list of individual permission values.</returns>
    private static List<PermissionType> GetIndividualPermissions(PermissionType permissions)
    {
        var result = new List<PermissionType>();

        if (permissions == PermissionType.None)
            return result;

        if (permissions.HasFlag(PermissionType.Clipboard))
            result.Add(PermissionType.Clipboard);

        if (permissions.HasFlag(PermissionType.FileSystem))
            result.Add(PermissionType.FileSystem);

        if (permissions.HasFlag(PermissionType.Network))
            result.Add(PermissionType.Network);

        if (permissions.HasFlag(PermissionType.Process))
            result.Add(PermissionType.Process);

        if (permissions.HasFlag(PermissionType.Storage))
            result.Add(PermissionType.Storage);

        if (permissions.HasFlag(PermissionType.Notifications))
            result.Add(PermissionType.Notifications);

        return result;
    }
}
