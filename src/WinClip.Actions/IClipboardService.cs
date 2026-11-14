namespace WinClip.Actions;

/// <summary>
/// Abstraction over system clipboard operations, enabling testability
/// without requiring a live WPF dispatcher or STA thread.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Sets the specified text on the system clipboard.
    /// </summary>
    void SetText(string text);
}
