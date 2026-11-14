using System;
using System.Collections.Generic;

namespace WinClip.SDK
{
    /// <summary>
    /// Defines the core interface that all WinClip plugins must implement.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets the unique identifier for this plugin.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the display name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the version of the plugin.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the description of what this plugin does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Initializes the plugin. Called once when the plugin is loaded.
        /// </summary>
        /// <param name="context">The clipboard context providing access to clipboard data.</param>
        void Initialize(IClipboardContext context);

        /// <summary>
        /// Determines whether this plugin should be enabled for the current clipboard content.
        /// </summary>
        /// <param name="context">The clipboard context containing the current clipboard data.</param>
        /// <returns>True if the plugin should be enabled; otherwise, false.</returns>
        bool ShouldEnable(IClipboardContext context);

        /// <summary>
        /// Gets the list of actions this plugin provides for the current clipboard content.
        /// </summary>
        /// <param name="context">The clipboard context containing the current clipboard data.</param>
        /// <returns>A collection of action identifiers and their display names.</returns>
        IEnumerable<PluginAction> GetActions(IClipboardContext context);

        /// <summary>
        /// Executes the specified action.
        /// </summary>
        /// <param name="actionId">The identifier of the action to execute.</param>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        void ExecuteAction(string actionId, IActionContext context);
    }

    /// <summary>
    /// Represents an action that a plugin can perform.
    /// </summary>
    public class PluginAction
    {
        /// <summary>
        /// Gets or sets the unique identifier for this action.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name shown in the menu.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional icon name or path for this action.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Gets or sets the optional keyboard shortcut hint (e.g., "Ctrl+T").
        /// </summary>
        public string? Shortcut { get; set; }
    }
}
