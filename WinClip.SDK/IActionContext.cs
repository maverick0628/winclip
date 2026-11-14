using System;
using System.Threading.Tasks;

namespace WinClip.SDK
{
    /// <summary>
    /// Provides context and operations available to plugins when executing actions.
    /// </summary>
    public interface IActionContext
    {
        /// <summary>
        /// Gets the clipboard context providing access to clipboard data.
        /// </summary>
        IClipboardContext Clipboard { get; }

        /// <summary>
        /// Sets new text content to the clipboard.
        /// </summary>
        /// <param name="text">The text to set on the clipboard.</param>
        void SetClipboard(string text);

        /// <summary>
        /// Pastes the current clipboard content at the cursor position.
        /// </summary>
        void Paste();

        /// <summary>
        /// Pastes the specified text at the cursor position.
        /// </summary>
        /// <param name="text">The text to paste.</param>
        void PasteText(string text);

        /// <summary>
        /// Opens the specified URL in the default web browser.
        /// </summary>
        /// <param name="url">The URL to open.</param>
        void OpenUrl(string url);

        /// <summary>
        /// Executes a shell command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The command arguments.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<ProcessResult> ExecuteCommandAsync(string command, string arguments = "");

        /// <summary>
        /// Displays a notification to the user.
        /// </summary>
        /// <param name="title">The notification title.</param>
        /// <param name="message">The notification message.</param>
        /// <param name="type">The notification type (info, warning, error).</param>
        void ShowNotification(string title, string message, NotificationType type = NotificationType.Info);

        /// <summary>
        /// Logs a message for debugging purposes.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log level.</param>
        void Log(string message, LogLevel level = LogLevel.Info);

        /// <summary>
        /// Gets the plugin's data directory for storing configuration or cache files.
        /// </summary>
        /// <param name="pluginId">The plugin identifier.</param>
        /// <returns>The absolute path to the plugin's data directory.</returns>
        string GetPluginDataDirectory(string pluginId);

        /// <summary>
        /// Dismisses the WinClip popup menu.
        /// </summary>
        void DismissPopup();
    }

    /// <summary>
    /// Represents the result of a process execution.
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// Gets or sets the exit code of the process.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Gets or sets the standard output from the process.
        /// </summary>
        public string Output { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the standard error output from the process.
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the process executed successfully.
        /// </summary>
        public bool Success => ExitCode == 0;
    }

    /// <summary>
    /// Defines the types of notifications that can be displayed.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Informational notification.
        /// </summary>
        Info,

        /// <summary>
        /// Warning notification.
        /// </summary>
        Warning,

        /// <summary>
        /// Error notification.
        /// </summary>
        Error,

        /// <summary>
        /// Success notification.
        /// </summary>
        Success
    }

    /// <summary>
    /// Defines logging levels for plugin messages.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug-level message.
        /// </summary>
        Debug,

        /// <summary>
        /// Informational message.
        /// </summary>
        Info,

        /// <summary>
        /// Warning message.
        /// </summary>
        Warning,

        /// <summary>
        /// Error message.
        /// </summary>
        Error
    }
}
