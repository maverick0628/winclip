using System.Text.Json;
using WinClip.Plugins.Security;

namespace WinClip.Plugins.API;

/// <summary>
/// Implementation of <see cref="IStorageApi"/> that enforces storage permission checks.
/// All storage operations require the Storage permission to be granted to the plugin.
/// Storage is scoped per-plugin to ensure data isolation.
/// </summary>
public class StorageApiImpl : IStorageApi
{
    private readonly PermissionType _grantedPermissions;
    private readonly string _pluginId;
    private readonly string _storageDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageApiImpl"/> class.
    /// </summary>
    /// <param name="pluginId">The unique identifier of the plugin.</param>
    /// <param name="grantedPermissions">The permissions that have been granted to the plugin.</param>
    /// <exception cref="ArgumentNullException">Thrown when pluginId is null or whitespace.</exception>
    public StorageApiImpl(string pluginId, PermissionType grantedPermissions)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            throw new ArgumentNullException(nameof(pluginId), "Plugin ID cannot be null or empty.");
        }

        _pluginId = pluginId;
        _grantedPermissions = grantedPermissions;

        // Set up plugin-specific storage directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageDirectory = Path.Combine(appDataPath, "WinClip", "PluginStorage", _pluginId);

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value)
    {
        // Enforce permission check before accessing storage
        PermissionGuard.RequirePermission(_grantedPermissions, PermissionType.Storage, "Storage Access");

        // Validate input
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Storage key cannot be null or empty.");
        }

        // Execute storage operation asynchronously
        await Task.Run(() =>
        {
            try
            {
                // Ensure storage directory exists
                Directory.CreateDirectory(_storageDirectory);

                // Serialize and save the value
                var filePath = GetStorageFilePath(key);
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                // Wrap storage exceptions with more context
                throw new InvalidOperationException($"Failed to store value with key '{key}'.", ex);
            }
        });
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key)
    {
        // Enforce permission check before accessing storage
        PermissionGuard.RequirePermission(_grantedPermissions, PermissionType.Storage, "Storage Access");

        // Validate input
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Storage key cannot be null or empty.");
        }

        // Execute storage operation asynchronously
        return await Task.Run(() =>
        {
            try
            {
                var filePath = GetStorageFilePath(key);

                // Return default if file doesn't exist
                if (!File.Exists(filePath))
                {
                    return default(T);
                }

                // Read and deserialize the value
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                // Wrap storage exceptions with more context
                throw new InvalidOperationException($"Failed to retrieve value with key '{key}'.", ex);
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string key)
    {
        // Enforce permission check before accessing storage
        PermissionGuard.RequirePermission(_grantedPermissions, PermissionType.Storage, "Storage Access");

        // Validate input
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Storage key cannot be null or empty.");
        }

        // Execute storage operation asynchronously
        return await Task.Run(() =>
        {
            try
            {
                var filePath = GetStorageFilePath(key);

                // Return false if file doesn't exist
                if (!File.Exists(filePath))
                {
                    return false;
                }

                // Delete the file
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                // Wrap storage exceptions with more context
                throw new InvalidOperationException($"Failed to delete value with key '{key}'.", ex);
            }
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key)
    {
        // Enforce permission check before accessing storage
        PermissionGuard.RequirePermission(_grantedPermissions, PermissionType.Storage, "Storage Access");

        // Validate input
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key), "Storage key cannot be null or empty.");
        }

        // Execute storage operation asynchronously
        return await Task.Run(() =>
        {
            try
            {
                var filePath = GetStorageFilePath(key);
                return File.Exists(filePath);
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                // Wrap storage exceptions with more context
                throw new InvalidOperationException($"Failed to check existence of key '{key}'.", ex);
            }
        });
    }

    /// <summary>
    /// Gets the file path for a storage key.
    /// Uses Path.GetFileName to prevent path traversal attacks.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>The full file path for the key.</returns>
    private string GetStorageFilePath(string key)
    {
        // SECURITY: Use Path.GetFileName to strip any path components
        // This prevents directory traversal attacks via malicious keys
        var fileName = Path.GetFileName(key);

        // If the key was entirely a path (e.g., "../../etc/passwd"),
        // Path.GetFileName returns empty string, so reject it
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                $"Invalid storage key '{key}': Key resulted in empty filename after path sanitization.",
                nameof(key));
        }

        // Additional sanitization: remove any remaining invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitizedFileName = string.Join("_", fileName.Split(invalidChars));

        // Ensure we still have a valid filename
        if (string.IsNullOrWhiteSpace(sanitizedFileName))
        {
            throw new ArgumentException(
                $"Invalid storage key '{key}': Key contains only invalid characters.",
                nameof(key));
        }

        // Use .json extension for all storage files
        if (!sanitizedFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            sanitizedFileName += ".json";
        }

        // Final security check: ensure the combined path stays within storage directory
        var fullPath = Path.Combine(_storageDirectory, sanitizedFileName);
        var normalizedFullPath = Path.GetFullPath(fullPath);
        var normalizedStorageDir = Path.GetFullPath(_storageDirectory);

        if (!normalizedFullPath.StartsWith(normalizedStorageDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Security violation: Storage key '{key}' attempted to escape plugin storage directory.");
        }

        return fullPath;
    }
}
