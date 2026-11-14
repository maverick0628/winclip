using WinClip.Models;

namespace WinClip.Services;

/// <summary>
/// Service interface for monitoring text selection events across Windows applications.
/// Uses low-level mouse hooks to detect when users select text.
/// </summary>
public interface ISelectionMonitorService
{
    /// <summary>
    /// Occurs when a text selection is detected.
    /// Fires when a left mouse button up event occurs, which typically indicates selection completion.
    /// </summary>
    event EventHandler<SelectionEvent>? SelectionDetected;

    /// <summary>
    /// Gets a value indicating whether the selection monitor is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the selection monitor service.
    /// Installs the low-level mouse hook and begins monitoring for selection events.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the service is already running.</exception>
    void Start();

    /// <summary>
    /// Stops the selection monitor service.
    /// Uninstalls the mouse hook and stops monitoring for selection events.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the service is not running.</exception>
    void Stop();
}
