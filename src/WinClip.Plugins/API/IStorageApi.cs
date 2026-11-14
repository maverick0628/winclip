namespace WinClip.Plugins.API;

/// <summary>
/// Provides persistent data storage functionality to plugins.
/// Requires Storage permission to use.
/// </summary>
public interface IStorageApi
{
    /// <summary>
    /// Stores a value with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of value to store.</typeparam>
    /// <param name="key">The key to associate with the value.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the plugin lacks Storage permission.</exception>
    Task SetAsync<T>(string key, T value);

    /// <summary>
    /// Retrieves a value with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve.</typeparam>
    /// <param name="key">The key of the value to retrieve.</param>
    /// <returns>The stored value, or default(T) if the key does not exist.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the plugin lacks Storage permission.</exception>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Deletes the value with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to delete.</param>
    /// <returns>True if the value was deleted; false if the key did not exist.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the plugin lacks Storage permission.</exception>
    Task<bool> DeleteAsync(string key);

    /// <summary>
    /// Checks if a value exists with the specified key.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if a value exists with the key; false otherwise.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the plugin lacks Storage permission.</exception>
    Task<bool> ExistsAsync(string key);
}
