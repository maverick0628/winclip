using System;

namespace WinClip.SDK
{
    /// <summary>
    /// Provides access to the current clipboard content and metadata.
    /// </summary>
    public interface IClipboardContext
    {
        /// <summary>
        /// Gets a value indicating whether the clipboard contains text data.
        /// </summary>
        bool HasText { get; }

        /// <summary>
        /// Gets a value indicating whether the clipboard contains image data.
        /// </summary>
        bool HasImage { get; }

        /// <summary>
        /// Gets a value indicating whether the clipboard contains file data.
        /// </summary>
        bool HasFiles { get; }

        /// <summary>
        /// Gets the text content from the clipboard, if available.
        /// </summary>
        /// <returns>The clipboard text, or null if no text is available.</returns>
        string? GetText();

        /// <summary>
        /// Gets the selected or highlighted text that triggered the popup, if available.
        /// This may differ from clipboard text in some contexts.
        /// </summary>
        /// <returns>The selected text, or null if no selection is available.</returns>
        string? GetSelectedText();

        /// <summary>
        /// Gets the file paths from the clipboard, if available.
        /// </summary>
        /// <returns>An array of file paths, or null if no files are available.</returns>
        string[]? GetFiles();

        /// <summary>
        /// Determines whether the clipboard contains data in the specified format.
        /// </summary>
        /// <param name="format">The data format to check for.</param>
        /// <returns>True if the clipboard contains data in the specified format; otherwise, false.</returns>
        bool ContainsFormat(string format);

        /// <summary>
        /// Gets data from the clipboard in the specified format.
        /// </summary>
        /// <param name="format">The data format to retrieve.</param>
        /// <returns>The clipboard data in the specified format, or null if not available.</returns>
        object? GetData(string format);

        /// <summary>
        /// Gets the name of the application that owns the current clipboard content.
        /// </summary>
        string? SourceApplication { get; }

        /// <summary>
        /// Gets the timestamp when the clipboard content was last modified.
        /// </summary>
        DateTime LastModified { get; }
    }
}
