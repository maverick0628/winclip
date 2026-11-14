namespace WinClip.Extensions;

/// <summary>
/// Provides a dictionary-based configuration store for extension settings.
/// Extensions can use this class to store and retrieve configuration values.
/// </summary>
public class ExtensionSettings
{
    private readonly Dictionary<string, object> _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionSettings"/> class.
    /// </summary>
    public ExtensionSettings()
    {
        _settings = new Dictionary<string, object>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionSettings"/> class with initial settings.
    /// </summary>
    /// <param name="initialSettings">The initial settings to populate the store.</param>
    public ExtensionSettings(Dictionary<string, object> initialSettings)
    {
        _settings = new Dictionary<string, object>(initialSettings);
    }

    /// <summary>
    /// Gets a setting value with the specified key.
    /// </summary>
    /// <typeparam name="T">The type to cast the setting value to.</typeparam>
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <returns>The setting value cast to the specified type, or default(T) if not found.</returns>
    public T? Get<T>(string key)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }

            // Attempt to convert if not directly castable
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        return default;
    }

    /// <summary>
    /// Gets a setting value with the specified key, or returns a default value if not found.
    /// </summary>
    /// <typeparam name="T">The type to cast the setting value to.</typeparam>
    /// <param name="key">The key of the setting to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the setting is not found.</param>
    /// <returns>The setting value cast to the specified type, or the default value if not found.</returns>
    public T GetOrDefault<T>(string key, T defaultValue)
    {
        var value = Get<T>(key);
        return value ?? defaultValue;
    }

    /// <summary>
    /// Sets a setting value for the specified key.
    /// </summary>
    /// <param name="key">The key of the setting to set.</param>
    /// <param name="value">The value to store.</param>
    public void Set(string key, object value)
    {
        _settings[key] = value;
    }

    /// <summary>
    /// Determines whether the settings store contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the settings store.</param>
    /// <returns>true if the settings store contains an element with the specified key; otherwise, false.</returns>
    public bool ContainsKey(string key)
    {
        return _settings.ContainsKey(key);
    }

    /// <summary>
    /// Removes the setting with the specified key from the settings store.
    /// </summary>
    /// <param name="key">The key of the setting to remove.</param>
    /// <returns>true if the setting was successfully removed; otherwise, false.</returns>
    public bool Remove(string key)
    {
        return _settings.Remove(key);
    }

    /// <summary>
    /// Removes all settings from the store.
    /// </summary>
    public void Clear()
    {
        _settings.Clear();
    }

    /// <summary>
    /// Gets all settings as a dictionary.
    /// </summary>
    /// <returns>A dictionary containing all current settings.</returns>
    public Dictionary<string, object> GetAll()
    {
        return new Dictionary<string, object>(_settings);
    }

    /// <summary>
    /// Gets the number of settings in the store.
    /// </summary>
    public int Count => _settings.Count;

    /// <summary>
    /// Gets all setting keys in the store.
    /// </summary>
    public IEnumerable<string> Keys => _settings.Keys;
}
