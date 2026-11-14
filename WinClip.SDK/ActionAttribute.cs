using System;

namespace WinClip.SDK
{
    /// <summary>
    /// Attribute to mark a method as a plugin action that can be executed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ActionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the action within the plugin.</param>
        /// <param name="displayName">The display name shown in the menu.</param>
        public ActionAttribute(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Action ID cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Action display name cannot be null or empty.", nameof(displayName));

            Id = id;
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the unique identifier for the action within the plugin.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the display name shown in the menu.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets or sets the optional icon name or path for this action.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Gets or sets the optional keyboard shortcut hint (e.g., "Ctrl+T").
        /// </summary>
        public string? Shortcut { get; set; }

        /// <summary>
        /// Gets or sets the sort order for this action in the menu (lower values appear first).
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether this action is enabled by default.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}
