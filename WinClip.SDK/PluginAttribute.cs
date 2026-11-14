using System;

namespace WinClip.SDK
{
    /// <summary>
    /// Attribute to mark a class as a WinClip plugin and provide metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PluginAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginAttribute"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the plugin.</param>
        /// <param name="name">The display name of the plugin.</param>
        /// <param name="version">The version of the plugin.</param>
        public PluginAttribute(string id, string name, string version)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Plugin ID cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Plugin name cannot be null or empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Plugin version cannot be null or empty.", nameof(version));

            Id = id;
            Name = name;
            Version = version;
        }

        /// <summary>
        /// Gets the unique identifier for the plugin.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the display name of the plugin.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the version of the plugin.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets or sets the description of what this plugin does.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the author or organization that created this plugin.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the URL to the plugin's homepage or documentation.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the path or name of the plugin's icon.
        /// </summary>
        public string? Icon { get; set; }
    }
}
