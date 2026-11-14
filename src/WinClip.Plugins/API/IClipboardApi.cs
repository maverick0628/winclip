namespace WinClip.Plugins.API;

/// <summary>
/// Provides clipboard access functionality to plugins.
/// Requires Clipboard permission to use.
/// </summary>
public interface IClipboardApi
{
    /// <summary>
    /// Gets the current text content from the clipboard.
    /// </summary>
    /// <returns>The text content from the clipboard, or null if clipboard contains no text.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the plugin lacks Clipboard permission.</exception>
    Task<string?> GetTextAsync();

    /// <summary>
    /// Sets text content to the clipboard.
    /// </summary>
    /// <param name="text">The text to copy to the clipboard.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the plugin lacks Clipboard permission.</exception>
    Task SetTextAsync(string text);

    /// <summary>
    /// Clears the clipboard content.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when the plugin lacks Clipboard permission.</exception>
    Task ClearAsync();
}
